using System;
using System.Linq;
using System.Reflection;

namespace HollywoodFX;

/// <summary>
/// Finding a private field whose name the deobfuscator may have moved.
///
/// SPT's assembly-tool renames obfuscated fields, and it derives the new name from the
/// field's own **type** name (ObfuscatedFieldRenamer.GetNewFieldNameFromTypeRename). A
/// field only qualifies if its name starts with one of the obfuscator's prefixes --
/// Class, GClass, Struct, GStruct, Interface, GInterface, Delegate, GDelegate, Exception,
/// GException, GControl, GAttribute, method, smethod, vmethod -- followed by a counter.
///
/// That splits the four fields this mod reaches into by string, and not the way the
/// names suggest:
///
///   `gdelegate64_0` starts with GDelegate, so it qualifies, and its type is now
///   `ShotDelegate` rather than `GDelegate64`. **That name is gone in 4.1.**
///
///   `lightAllocationPoolClass`, `dictionary_0`, `dictionary_2` and the muzzle arrays do
///   not start with any of those prefixes, so the renamer never touches them, whatever
///   happened to their types. They should still be there.
///
/// "Should" is doing work in that second paragraph, though, and the failure is invisible
/// to the compiler either way: the name is a string, so the build passes and the mod
/// fails in a raid.
///
/// Deobfuscation is also what makes it recoverable. The field's **type** is the part that
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
