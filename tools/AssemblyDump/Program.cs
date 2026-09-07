using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Collections.Immutable;
using System.Text;

// Usage: dotnet run -- "<path to Assembly-CSharp.dll>" [name ...]
//
// With no names it writes types.txt, every type in the assembly, one full name per
// line. With names it also writes members.txt: for each type whose full name contains
// one of them, every field, method and property it declares.
//
// Nothing is loaded or executed. This reads the metadata tables directly, which is
// what makes it safe to point at a game assembly and why it needs no references.

if (args.Length == 0)
{
    Console.Error.WriteLine("""
        usage:
          dotnet run -- --scan "<SPT install root>"
              Finds every Assembly-CSharp.dll under the root and says which one SPT
              actually deobfuscated. Start here if a build cannot find types that the
              mapping tables say exist.

          dotnet run -- "<Assembly-CSharp.dll>" [name to detail ...]
              types.txt for every type; members.txt for the types you name.

        example:
          dotnet run -- --scan "E:\SPT 4.1"
          dotnet run -- "E:\SPT 4.1\SPT_Runtime\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll" TarkovApplication
        """);
    return 1;
}

// --- scan -------------------------------------------------------------------------
//
// An SPT install can hold more than one Assembly-CSharp.dll, and only one of them is
// the one SPT patched. Referencing the other compiles against BSG's raw names, where
// nothing SPT renamed exists and most members are unprintable unicode, so every type
// the migration tables promise comes back "not found" and the tables look wrong.
//
// Telling them apart needs no name list: the raw assembly is full of members whose
// names are unprintable, and the patched one is not.
if (args[0] is "--scan")
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("usage: dotnet run -- --scan \"<SPT install root>\"");
        return 1;
    }

    var root = args[1];

    if (!Directory.Exists(root))
    {
        Console.Error.WriteLine($"not a folder: {root}");
        return 1;
    }

    var found = Directory.EnumerateFiles(root, "Assembly-CSharp.dll", SearchOption.AllDirectories).ToList();

    if (found.Count == 0)
    {
        Console.Error.WriteLine($"no Assembly-CSharp.dll anywhere under {root}");
        return 1;
    }

    foreach (var candidate in found)
    {
        Console.WriteLine(candidate);

        try
        {
            using var s = File.OpenRead(candidate);
            using var p = new PEReader(s);

            if (!p.HasMetadata)
            {
                Console.WriteLine("    not a .NET assembly");
                continue;
            }

            var r = p.GetMetadataReader();
            var total = 0;
            var unprintable = 0;

            foreach (var th in r.TypeDefinitions)
            {
                var td = r.GetTypeDefinition(th);

                foreach (var mh in td.GetMethods())
                {
                    total++;
                    var n = r.GetString(r.GetMethodDefinition(mh).Name);

                    if (n.Length == 0 || n.Any(c => char.IsControl(c) || c > 0x2000))
                    {
                        unprintable++;
                    }
                }
            }

            var share = total == 0 ? 0 : unprintable * 100.0 / total;

            Console.WriteLine($"    {r.TypeDefinitions.Count:N0} types, {share:F0}% of methods have unprintable names");
            Console.WriteLine(share > 5
                ? "    >>> RAW, still obfuscated. Do NOT build against this one."
                : "    >>> deobfuscated. This is the one to build against.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    could not read: {ex.Message}");
        }

        Console.WriteLine();
    }

    return 0;
}

var path = args[0];

if (!File.Exists(path))
{
    Console.Error.WriteLine($"not found: {path}");
    return 1;
}

using var stream = File.OpenRead(path);
using var pe = new PEReader(stream);

if (!pe.HasMetadata)
{
    Console.Error.WriteLine($"{path} has no .NET metadata (native dll?)");
    return 1;
}

var md = pe.GetMetadataReader();

string FullName(TypeDefinition t)
{
    var name = md.GetString(t.Name);
    var ns = md.GetString(t.Namespace);

    // Nested types carry no namespace of their own; walk up to the declaring type.
    if (t.IsNested)
    {
        var parent = md.GetTypeDefinition(t.GetDeclaringType());
        return $"{FullName(parent)}+{name}";
    }

    return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
}

var types = md.TypeDefinitions
    .Select(h => (Handle: h, Def: md.GetTypeDefinition(h)))
    .Select(x => (x.Handle, x.Def, Name: FullName(x.Def)))
    .OrderBy(x => x.Name, StringComparer.Ordinal)
    .ToList();

File.WriteAllLines("types.txt", types.Select(t => t.Name));
Console.WriteLine($"types.txt   {types.Count:N0} types");

if (args.Length == 1)
{
    return 0;
}

// Obfuscated names are unprintable, which prints as nothing and reads as a bug. Show
// them as escapes so "this member has no name you can type" is visible.
string Name(StringHandle h)
{
    var raw = md.GetString(h);

    return raw.Any(c => char.IsControl(c) || c > 0x2000)
        ? "\\u" + string.Join("\\u", raw.Select(c => ((int)c).ToString("x4")))
        : raw;
}

var wanted = args.Skip(1).ToArray();
var sb = new StringBuilder();
var matched = 0;

foreach (var (_, def, name) in types)
{
    if (!wanted.Any(w => name.Contains(w, StringComparison.OrdinalIgnoreCase)))
    {
        continue;
    }

    matched++;
    sb.AppendLine($"=== {name}");

    var sig = new SignatureNames();

    foreach (var fh in def.GetFields())
    {
        var f = md.GetFieldDefinition(fh);
        var type = f.DecodeSignature(sig, null);
        sb.AppendLine($"    field    {type} {Name(f.Name)}   [{f.Attributes}]");
    }

    foreach (var ph in def.GetProperties())
    {
        sb.AppendLine($"    property {Name(md.GetPropertyDefinition(ph).Name)}");
    }

    foreach (var mh in def.GetMethods())
    {
        var m = md.GetMethodDefinition(mh);
        var s = m.DecodeSignature(sig, null);

        // The signature is the part that survives a rename, so it is what a patch
        // target is actually identified by once the number has moved.
        sb.AppendLine(
            $"    method   {s.ReturnType} {Name(m.Name)}({string.Join(", ", s.ParameterTypes)})"
            + $"   [{m.Attributes & MethodAttributes.MemberAccessMask}]");
    }

    sb.AppendLine();
}

File.WriteAllText("members.txt", sb.ToString());
Console.WriteLine($"members.txt {matched:N0} types matched {string.Join(", ", wanted)}");
return 0;


/// <summary>
/// Renders a signature blob as readable type names.
///
/// System.Reflection.Metadata hands back handles rather than names, so decoding a
/// signature needs somebody to say what each type is called. Nothing here resolves
/// across assemblies: a type from another assembly is printed from its own reference
/// row, which is all that is needed to recognise a method by shape.
/// </summary>
internal sealed class SignatureNames : ISignatureTypeProvider<string, object>
{
    public string GetPrimitiveType(PrimitiveTypeCode code) => code switch
    {
        PrimitiveTypeCode.Boolean => "bool",
        PrimitiveTypeCode.Byte => "byte",
        PrimitiveTypeCode.Char => "char",
        PrimitiveTypeCode.Double => "double",
        PrimitiveTypeCode.Int16 => "short",
        PrimitiveTypeCode.Int32 => "int",
        PrimitiveTypeCode.Int64 => "long",
        PrimitiveTypeCode.Object => "object",
        PrimitiveTypeCode.SByte => "sbyte",
        PrimitiveTypeCode.Single => "float",
        PrimitiveTypeCode.String => "string",
        PrimitiveTypeCode.UInt16 => "ushort",
        PrimitiveTypeCode.UInt32 => "uint",
        PrimitiveTypeCode.UInt64 => "ulong",
        PrimitiveTypeCode.Void => "void",
        PrimitiveTypeCode.IntPtr => "nint",
        PrimitiveTypeCode.UIntPtr => "nuint",
        PrimitiveTypeCode.TypedReference => "TypedReference",
        _ => code.ToString(),
    };

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var def = reader.GetTypeDefinition(handle);
        return Readable(reader.GetString(def.Name));
    }

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var reference = reader.GetTypeReference(handle);
        return Readable(reader.GetString(reference.Name));
    }

    public string GetTypeFromSpecification(
        MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public string GetSZArrayType(string elementType) => elementType + "[]";

    public string GetArrayType(string elementType, ArrayShape shape) =>
        elementType + "[" + new string(',', Math.Max(0, shape.Rank - 1)) + "]";

    public string GetByReferenceType(string elementType) => "ref " + elementType;

    public string GetPointerType(string elementType) => elementType + "*";

    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
        $"{genericType}<{string.Join(", ", typeArguments)}>";

    public string GetGenericMethodParameter(object genericContext, int index) => "!!" + index;

    public string GetGenericTypeParameter(object genericContext, int index) => "!" + index;

    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;

    public string GetPinnedType(string elementType) => elementType;

    public string GetFunctionPointerType(MethodSignature<string> signature) => "delegate*";

    private static string Readable(string name) =>
        name.Any(c => char.IsControl(c) || c > 0x2000)
            ? "\\u" + string.Join("\\u", name.Select(c => ((int)c).ToString("x4")))
            : name;
}
