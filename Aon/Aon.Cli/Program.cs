using Ambermoon.Aon;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: aod <file.aod>");
    return 1;
}

var path = args[0];
if (!File.Exists(path))
{
    Console.Error.WriteLine($"File not found: {path}");
    return 1;
}

AodParseResult result;
try
{
    result = AodFileParser.ParseFile(path);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to read file: {ex.Message}");
    return 1;
}

foreach (var d in result.Diagnostics)
    (d.Severity == DiagnosticSeverity.Error ? Console.Error : Console.Out).WriteLine(d);

if (!result.Success)
    return 1;

var file = result.File!;
bool first = true;

foreach (var def in file.Definitions)
{
    if (!first) Console.WriteLine();
    first = false;

    switch (def)
    {
        case EnumDef e:
            PrintEnum(e);
            break;
        case BitfieldDef b:
            PrintBitfield(b);
            break;
        case StructDef s:
            PrintStruct(s);
            break;
    }
}

return 0;

// ---------------------------------------------------------------

static void PrintEnum(EnumDef e)
{
    Console.WriteLine($"enum {e.Name} : {PrimName(e.UnderlyingType)} {{");

    int next = 0;
    foreach (var m in e.Members)
    {
        int value = m.ExplicitValue ?? next;
        Console.WriteLine($"    {m.Name} = {value}");
        next = value + 1;
    }

    Console.WriteLine("}");
}

static void PrintBitfield(BitfieldDef b)
{
    Console.WriteLine($"bitfield {b.Name} : {PrimName(b.UnderlyingType)} {{");

    for (int i = 0; i < b.Flags.Count; i++)
        Console.WriteLine($"    {b.Flags[i],-24} // 0x{1 << i:X2}");

    Console.WriteLine("}");
}

static void PrintStruct(StructDef s)
{
    var header = s.BaseType != null
        ? $"struct {s.Name} : {s.BaseType}"
        : $"struct {s.Name}";
    Console.WriteLine($"{header} {{");

    foreach (var m in s.Members)
    {
        switch (m)
        {
            case OffsetDirective od:
                Console.WriteLine($"    [0x{od.Offset:X4}]");
                break;
            case FieldFixup ff:
                Console.WriteLine($"    {ff.FieldName} = {ff.Value}");
                break;
            case FieldDecl fd:
                var over = fd.IsOverride ? "new " : "";
                Console.WriteLine($"    {over}{TypeName(fd.Type),-32} {fd.Name}");
                break;
        }
    }

    Console.WriteLine("}");
}

static string TypeName(TypeRef t) => t switch
{
    PrimitiveTypeRef p => p.ArraySize.HasValue
        ? $"{PrimName(p.Type)}[{p.ArraySize}]"
        : PrimName(p.Type),
    NamedTypeRef n => n.ArraySize.HasValue
        ? $"{n.TypeName}[{n.ArraySize}]"
        : n.TypeName,
    _ => "?",
};

static string PrimName(PrimitiveType t) => t switch
{
    PrimitiveType.UByte  => "ubyte",
    PrimitiveType.SByte  => "sbyte",
    PrimitiveType.UWord  => "uword",
    PrimitiveType.SWord  => "sword",
    PrimitiveType.UDWord => "udword",
    PrimitiveType.SDWord => "sdword",
    _ => t.ToString(),
};
