using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;

namespace HollywoodFX.Muzzle;

internal class CurrentShot
{
    public bool Handled = true;

    public AmmoItemClass Ammo;
    public bool Silenced;
}

internal class MuzzleState
{
    public Transform Fireport;
    public readonly List<MuzzleJet> Jets = [];
    public readonly List<MuzzleFume> Smokes = [];
    public MuzzleSmoke[] Trails;
    public Weapon Weapon;
    public IPlayer Player;
    
    public float Time;
    public float TimeSmokeEmitted = 0f;
    public float TimeSmokeThreshold = 0.25f;
}

internal class MuzzleStatic
{
    public readonly CurrentShot CurrentShot = new();
    private readonly Dictionary<int, MuzzleState> _muzzleStates = new();

    // The _0 suffix is the obfuscator's, which means these names belong to a build of the
    // game rather than to the game. Resolved through Field so a rename after an update
    // says so once and leaves the muzzle plain, instead of throwing on every shot fired
    // for the rest of the raid.
    private static readonly FieldInfo JetField = Field("muzzleJet_0");
    private static readonly FieldInfo FumeField = Field("muzzleFume_0");
    private static readonly FieldInfo SmokeField = Field("muzzleSmoke_0");

    private static FieldInfo Field(string name)
    {
        var field = typeof(MuzzleManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

        if (field == null)
        {
            Plugin.Log.LogError(
                $"[HollywoodFX] MuzzleManager.{name} is gone in this build of the game, so muzzle "
                + "effects are off. The name is the obfuscator's and moves when BSG changes the class.");
        }

        return field;
    }

    public bool TryGetMuzzleState(MuzzleManager manager, out MuzzleState state)
    {
        var managerId = manager.gameObject.transform.GetInstanceID();
        return _muzzleStates.TryGetValue(managerId, out state);
    }

    public void UpdateCurrentShot(AmmoItemClass ammo, bool silenced)
    {
        CurrentShot.Handled = false;
        CurrentShot.Ammo = ammo;
        CurrentShot.Silenced = silenced;
    }

    public MuzzleState UpdateMuzzleState(MuzzleManager manager, Weapon weapon, IPlayer player)
    {
        // Grab the fireport of the current weapon
        Transform fireport = null;

        var children = manager.Hierarchy.GetComponentsInChildren<Transform>();
        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];

            if (child.name == "fireport")
                fireport = child;
        }

        if (fireport == null)
            return null;

        var fireportDir = -1 * fireport.up;

        if (FumeField?.GetValue(manager) is not MuzzleFume[] fumes)
            return null;

        var managerId = manager.gameObject.transform.GetInstanceID();

        // Get or create the muzzle state entry for this manager
        if (!_muzzleStates.TryGetValue(managerId, out var state))
        {
            _muzzleStates[managerId] = state = new MuzzleState();
            state.Time = Time.unscaledTime;
        }

        // It seems like the smoke emitters are the best way to determine the actual muzzle opening as the fireport doesn't take silencers into account
        const float fwdAngle = 25f;
        var candidateAngle = 10f;

        state.Smokes.Clear();

        for (var i = 0; i < fumes.Length; i++)
        {
            var fume = fumes[i];

            var angle = Vector3.Angle(fireportDir, -1 * fume.transform.up);

            // Add the non-forward fume components
            if (angle > fwdAngle)
            {
                state.Smokes.Add(fume);
            }

            if (angle > candidateAngle)
                continue;

            fireport = fume.transform;
            candidateAngle = angle;
        }

        state.Fireport = fireport;
        state.Weapon = weapon;
        state.Player = player;

        // Update the jets
        state.Jets.Clear();

        if (JetField?.GetValue(manager) is MuzzleJet[] jets)
        {
            for (var i = 0; i < jets.Length; i++)
            {
                var jet = jets[i];
                var jetDir = -1 * jet.transform.up;
                var jetAngle = Vector3.Angle(jetDir, fireportDir);

                if (jetAngle > 20)
                    state.Jets.Add(jet);
            }
        }

        // Update the smokes
        state.Trails = SmokeField?.GetValue(manager) as MuzzleSmoke[];

        return state;
    }
}