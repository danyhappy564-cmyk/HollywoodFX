using System.Reflection;
using EFT.AssetsManager;
using SPT.Reflection.Patching;

namespace HollywoodFX.Patches;

public class AmmoPoolObjectAutoDestroyPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(AmmoPoolObject).GetMethod(nameof(AmmoPoolObject.StartAutoDestroyCountDown));
    }

    [PatchPostfix]
    // ReSharper disable InconsistentNaming
    // ___c is the countdown field, called float_0 before 4.1 named it. A single letter is
    // an odd name to depend on, but it is the only float on AmmoPoolObject, so there is
    // nothing else it could be. Harmony binds these by name at patch time, which is why
    // getting it wrong is not a build error.
    private static void Postfix(AmmoPoolObject __instance, ref float ___c)
    {
        ___c = Plugin.MiscShellLifetime.Value;
        __instance.Shell.transform.localScale *= Plugin.MiscShellSize.Value;
    }
}
