using System.Collections.Generic;
using DeferredDecals;
using EFT.Ballistics;
using HarmonyLib;
using UnityEngine;

namespace HollywoodFX.Decal;

public class DecalPainter
{
    private readonly DeferredDecalRenderer _renderer;

    private readonly Dictionary<Material, DeferredDecalRenderer.ManagedMesh> _dictionary0;
    private readonly Dictionary<Camera, DeferredDecalRenderer.CameraData> _dictionary2;
    
    public DecalPainter(DeferredDecalRenderer renderer)
    {
        _renderer = renderer;
        // dictionary_0 and dictionary_2 are the obfuscator counting, so the numbers move.
        // What does not move is what each one holds, and after 4.1 deobfuscated those
        // (ManagedMesh, CameraData) the value type tells the two apart on its own.
        // See ObfuscatedField.
        _dictionary0 = (Dictionary<Material, DeferredDecalRenderer.ManagedMesh>)ObfuscatedField
            .Find(typeof(DeferredDecalRenderer),
                  typeof(Dictionary<Material, DeferredDecalRenderer.ManagedMesh>),
                  "dictionary_0")
            ?.GetValue(_renderer);

        _dictionary2 = (Dictionary<Camera, DeferredDecalRenderer.CameraData>)ObfuscatedField
            .Find(typeof(DeferredDecalRenderer),
                  typeof(Dictionary<Camera, DeferredDecalRenderer.CameraData>),
                  "dictionary_2")
            ?.GetValue(_renderer);
    }

    public void DrawDecal(
        DeferredDecalRenderer.SingleDecal decal,
        Vector3 position,
        Vector3 normal,
        BallisticCollider hitCollider,
        float projectorHeight=0.1f)
    {
        // Null only if the fields could not be found at all, which ObfuscatedField has
        // already said once. Decals are then simply not drawn, rather than throwing on
        // every bullet that lands.
        if (_dictionary0 == null || _dictionary2 == null)
            return;

        if (!_dictionary0.ContainsKey(decal.DecalMaterial))
        {
            foreach (var keyValuePair in _dictionary2)
                keyValuePair.Value.IsStaticBufferDirty = true;
            _renderer.method_7(decal);
        }
        _renderer.method_6(position, normal, _dictionary0[decal.DecalMaterial], decal, projectorHeight);
    }
}