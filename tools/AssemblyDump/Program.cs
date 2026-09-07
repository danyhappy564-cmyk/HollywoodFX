using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
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
        usage: dotnet run -- "<Assembly-CSharp.dll>" [name to detail ...]

        example:
          dotnet run -- "E:\SPT 4.1\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll" TarkovApplication LampController
        """);
    return 1;
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

    foreach (var fh in def.GetFields())
    {
        var f = md.GetFieldDefinition(fh);
        sb.AppendLine($"    field    {md.GetString(f.Name)}   [{f.Attributes}]");
    }

    foreach (var ph in def.GetProperties())
    {
        sb.AppendLine($"    property {md.GetString(md.GetPropertyDefinition(ph).Name)}");
    }

    foreach (var mh in def.GetMethods())
    {
        var m = md.GetMethodDefinition(mh);
        sb.AppendLine($"    method   {md.GetString(m.Name)}   [{m.Attributes & MethodAttributes.MemberAccessMask}]");
    }

    sb.AppendLine();
}

File.WriteAllText("members.txt", sb.ToString());
Console.WriteLine($"members.txt {matched:N0} types matched {string.Join(", ", wanted)}");
return 0;
