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
        // These were dictionary_0 and dictionary_2, the obfuscator counting. 4.1 named
        // them _meshesDict and _cameras. Still resolved through ObfuscatedField, which
        // falls back to matching on the value type, since that is what told the two apart
        // when the names were numbers and is what will again if they move. See
        // ObfuscatedField.
        _dictionary0 = (Dictionary<Material, DeferredDecalRenderer.ManagedMesh>)ObfuscatedField
            .Find(typeof(DeferredDecalRenderer),
                  typeof(Dictionary<Material, DeferredDecalRenderer.ManagedMesh>),
                  "_meshesDict")
            ?.GetValue(_renderer);

        _dictionary2 = (Dictionary<Camera, DeferredDecalRenderer.CameraData>)ObfuscatedField
            .Find(typeof(DeferredDecalRenderer),
                  typeof(Dictionary<Camera, DeferredDecalRenderer.CameraData>),
                  "_cameras")
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
            _renderer.CreateDecalMesh(decal);
        }
        _renderer.AddCubeToMesh(position, normal, _dictionary0[decal.DecalMaterial], decal, projectorHeight);
    }
}