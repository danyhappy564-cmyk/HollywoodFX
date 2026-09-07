using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using HarmonyLib;
using HollywoodFX.Gore;
using SPT.Reflection.Patching;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Patches;

public class ShotDelegateWrapperPatch : ModulePatch
{
    public static ShotDelegate OriginalShotDelegate;
    
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(GameWorld __instance)
    {
        if (GameWorldAwakePrefixPatch.IsHideout)
            return;

        var ballistics = __instance.gameObject.GetComponent<BallisticsCalculator>();
        
        Plugin.Log.LogInfo("Getting the shot delegate field from BallisticsCalculator");

        // gdelegate64_0 in 4.0, when the type was GDelegate64. Both moved together: the
        // type is ShotDelegate and the field is _shotDelegate, and it is the only field of
        // that type on BallisticsCalculator, so the fallback would find it either way.
        var shotDelegateField = ObfuscatedField.Find(
            typeof(BallisticsCalculator), typeof(ShotDelegate), "_shotDelegate");

        if (shotDelegateField == null)
        {
            // Every impact, gore and tracer effect hangs off this delegate, so losing it
            // is worth saying plainly rather than leaving as a mod that does nothing.
            Plugin.Log.LogError("[HollywoodFX] no shot delegate to wrap, so shot effects are off.");
            return;
        }

        OriginalShotDelegate = (ShotDelegate)shotDelegateField.GetValue(ballistics);
        Plugin.Log.LogInfo($"Original shot delegate retrieved: {OriginalShotDelegate.Method}");
        shotDelegateField.SetValue(ballistics, new ShotDelegate(OnShot));
        Plugin.Log.LogInfo("Replaced the shot delegate with internal HFX override");
    }
    
    /*
     * This has to be handled here because we must stash player hits before the player gets killed in the ClientGameWorld.ShotDelegate
     * Furthermore, Fika now overrides the ShotDelegate method and doesn't call the base class, which means we have to hook in before ShotDelegate
     * is called at all.
     */
    private static void OnShot(Shot shotResult)
    {
        var bullet = ImpactStatic.Kinetics.Bullet;

        bullet.Update(shotResult);
        
        var hitCollider = bullet.Info.HitCollider;

        if (hitCollider != null && bullet.HitColliderRoot.gameObject.layer == LayersMaskController.PlayerLayer)
        {
            Singleton<PlayerDamageRegistry>.Instance.RegisterDamage(ImpactStatic.Kinetics.Bullet, hitCollider, bullet.HitColliderRoot);            
        }
        
        OriginalShotDelegate(shotResult);
    }
}

public class EffectsEmitPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Need to disambiguate the correct emit method
        return typeof(Effects).GetMethod(nameof(Effects.Emit),
        [
            typeof(MaterialType), typeof(BallisticCollider), typeof(Vector3), typeof(Vector3), typeof(float),
            typeof(bool), typeof(bool), typeof(EPointOfView)
        ]);
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(Effects __instance, MaterialType material, BallisticCollider hitCollider,
        Vector3 position, Vector3 normal, float volume, bool isKnife, bool isHitPointVisible, EPointOfView pov)
    {
        if (GameWorldAwakePrefixPatch.IsHideout || isKnife)
            return;

        ImpactStatic.Kinetics.Update(material, position, normal, isHitPointVisible);
        Singleton<ImpactController>.Instance.Emit(ImpactStatic.Kinetics);
    }
}