using System;
using System.Linq;
using System.Reflection;

namespace HollywoodFX;

/// <summary>
/// Finding a private field whose name belonged to the obfuscator.
///
/// A few of the fields this mod reaches into were named after their own type by the
/// obfuscator: `gdelegate64_0` on BallisticsCalculator, `lightAllocationPoolClass` on
/// Effects, `dictionary_0` and `dictionary_2` on DeferredDecalRenderer. 4.1 deobfuscates
/// the client, and those types are now `ShotDelegate`, `LightPool`, `ManagedMesh` and
/// `CameraData`. A field named after a type that has been renamed is a field that has
/// very likely been renamed with it.
///
/// That is the worst kind of break to inherit, because it is invisible to the compiler:
/// the name is a string, so the build passes and the mod fails in a raid.
///
/// Deobfuscation is also what makes it fixable. The field's **type** is the part that
/// does not move, and on these classes it identifies the field on its own. So look the
/// name up first, since that is free and exact when it still works, and otherwise find
/// the one field of the right type. Only when neither works is anything actually wrong,
/// and then it says so once, by name, instead of throwing somewhere further along.
/// </summary>
internal static class ObfuscatedField
{
    private const BindingFlags Instance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <param name="nameIn40">What the field was called in 4.0. Tried first.</param>
    internal static FieldInfo Find(Type owner, Type fieldType, string nameIn40)
    {
        var byName = owner.GetField(nameIn40, Instance);

        if (byName != null && fieldType.IsAssignableFrom(byName.FieldType))
        {
            return byName;
        }

        var byType = owner.GetFields(Instance)
            .Where(f => fieldType.IsAssignableFrom(f.FieldType))
            .ToArray();

        if (byType.Length == 1)
        {
            Plugin.Log.LogInfo(
                $"[HollywoodFX] {owner.Name}.{nameIn40} is not in this build; matched "
                + $"{owner.Name}.{byType[0].Name} on its type ({fieldType.Name}) instead.");

            return byType[0];
        }

        Plugin.Log.LogError(
            $"[HollywoodFX] Could not find {owner.Name}.{nameIn40}, and {byType.Length} fields "
            + $"of type {fieldType.Name} on {owner.Name} means the type cannot pick it out either. "
            + "Whatever uses this field is now off.");

        return null;
    }
}
