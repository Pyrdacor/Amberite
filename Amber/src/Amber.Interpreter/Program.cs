using Amber.Interpreter.AST;
using Amber.Interpreter.Runtime;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: amber <script.amb> [--dump]");
    return 1;
}

var filePath  = args[0];
var dumpState = args.Contains("--dump");

if (!File.Exists(filePath))
{
    Console.Error.WriteLine($"amber: file not found: {filePath}");
    return 1;
}

AmberInterpreter interpreter;
try
{
    interpreter = AmberRunner.Execute(File.ReadAllText(filePath), filePath);
}
catch (AmberParseException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
catch (AmberRuntimeException ex)
{
    Console.Error.WriteLine($"Runtime error: {ex.Message}");
    return 1;
}

if (dumpState)
{
    DumpTypes(interpreter.Types);
    DumpVariables(interpreter.Environment);
}

return 0;

static void DumpTypes(TypeRegistry types)
{
    var all = types.All().ToList();
    if (all.Count == 0) return;

    Console.WriteLine("--- types ---");
    foreach (var info in all)
    {
        switch (info)
        {
            case EnumTypeInfo e:
                Console.WriteLine($"  enum {e.Name} : {e.BaseType.ToString().ToLower()}");
                foreach (var (name, value) in e.Members)
                    Console.WriteLine($"    {name,-20} = {value}  (0x{value:X})");
                break;

            case BitfieldTypeInfo b:
                Console.WriteLine($"  bitfield {b.Name} : {b.BaseType.ToString().ToLower()}");
                foreach (var (name, value) in b.Members)
                    Console.WriteLine($"    {name,-20} = 0x{value:X}");
                break;

            case StructTypeInfo s:
                Console.WriteLine($"  struct {s.Name}");
                foreach (var f in s.Fields)
                {
                    var arraySuffix = f.ArraySize.HasValue ? $"[{f.ArraySize}]" : "";
                    Console.WriteLine($"    {FieldTypeRefName(f.Type)} {f.Name}{arraySuffix}");
                }
                break;

            case FunctionInfo fn:
                var paramStr = string.Join(", ", fn.Parameters.Select(p =>
                    $"{p.Direction.ToString().ToLower()} {p.Register}: {FieldTypeRefName(p.Type)} {p.Name}"));
                Console.WriteLine($"  function {fn.Name}({paramStr})");
                break;
        }
    }
}

static string FieldTypeRefName(FieldTypeRef t) => t switch
{
    PrimitiveTypeRef p => p.Type.ToString().ToLower(),
    UserTypeRef      u => u.TypeName,
    PointerTypeRef   p => $"Ptr<{FieldTypeRefName(p.ElementType)}>",
    _ => "?"
};

static void DumpVariables(AmberEnvironment env)
{
    var all = env.All().OrderBy(x => x.Name).ToList();
    if (all.Count == 0) return;

    Console.WriteLine("--- variables ---");
    foreach (var (name, type, value) in all)
        Console.WriteLine($"  {type.ToString().ToLower(),-5} {name,-16} = {value}");
}
