using Ambdev.Interpreter.AST;
using Ambdev.Interpreter.Runtime;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: ambdev <file.ambdev> [--dump]");
    return 1;
}

var path = args[0];
if (!File.Exists(path))
{
    Console.Error.WriteLine($"File not found: {path}");
    return 1;
}

var dump = args.Contains("--dump");

try
{
    var source      = File.ReadAllText(path);
    var interpreter = AmbdevRunner.Execute(source, path);

    if (dump)
    {
        var reg = interpreter.Registry;

        foreach (var (_, e) in reg.Enums.OrderBy(p => p.Key))
        {
            Console.WriteLine($"enum {e.Name} : {e.BaseType.ToString().ToLower()}");
            foreach (var (name, value) in e.Members)
                Console.WriteLine($"  {name} = {value}");
        }

        foreach (var (_, b) in reg.Bitfields.OrderBy(p => p.Key))
        {
            Console.WriteLine($"bitfield {b.Name} : {b.BaseType.ToString().ToLower()}");
            foreach (var (name, value) in b.Members)
                Console.WriteLine($"  {name} = 0x{value:X}");
        }

        foreach (var (_, e) in reg.Etypes.OrderBy(p => p.Key))
        {
            Console.WriteLine($"etype[{e.Index}, {e.Name}]:");
            foreach (var f in e.Fields)
                Console.WriteLine($"  - {f.Offset}: {FormatType(f.Type)}{(f.IsOptional ? "?" : "")} {f.Name}{FormatDefault(f.DefaultValue)}{FormatRange(f.Range)}");
        }

        foreach (var (_, es) in reg.Especs.OrderBy(p => p.Key))
        {
            Console.WriteLine($"espec[{es.EtypeIndex}, {es.Name}]:");
            foreach (var c in es.Conditions)
                Console.WriteLine($"  - when {FormatCondition(c)}");
            foreach (var f in es.Fields)
                Console.WriteLine($"  - {f.Offset}: {FormatType(f.Type)}{(f.IsOptional ? "?" : "")} {f.Name}{FormatDefault(f.DefaultValue)}{FormatRange(f.Range)}");
        }

        foreach (var (_, ev) in reg.Events.OrderBy(p => p.Key))
        {
            Console.WriteLine($"event[{ev.Index}, {ev.Name}] = etype[{ev.EtypeIndex}]");
            foreach (var f in ev.Fields)
                Console.WriteLine($"  - {f.Name}: {f.Value}");
        }

        foreach (var (_, c) in reg.Chains.OrderBy(p => p.Key))
        {
            Console.WriteLine($"chain[{c.Index}, {c.Name}]");
            foreach (var s in c.Steps)
            {
                var prefix = s.Prefix switch
                {
                    StepPrefix.Always    => "-",
                    StepPrefix.IfSuccess => "?",
                    StepPrefix.IfFailure => "!",
                    _ => "?"
                };
                Console.WriteLine($"  {prefix} {(s.Target == StepTarget.Event ? "event" : "chain")} {s.TargetIndex}");
            }
        }
    }

    return 0;
}
catch (AmbdevParseException ex)
{
    Console.Error.WriteLine($"Parse error: {ex.Message}");
    return 1;
}
catch (AmbdevRuntimeException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static string FormatType(AmbdevTypeRef t) => t switch
{
    PrimitiveTypeRef p => p.Type.ToString().ToLower(),
    NamedTypeRef     n => n.TypeName,
    _                  => "?"
};

static string FormatDefault(long? v) => v.HasValue ? $" = {v}" : "";

static string FormatRange(RangeConstraint? r) => r switch
{
    ContinuousRange c => $" [{c.Min}..{c.Max}]",
    ValueListRange  v => $" [{string.Join(", ", v.Values)}]",
    _                 => ""
};

static string FormatCondition(ConditionInfo c) => c switch
{
    AndConditionInfo a     => $"({FormatCondition(a.Left)} and {FormatCondition(a.Right)})",
    OrConditionInfo  o     => $"({FormatCondition(o.Left)} or {FormatCondition(o.Right)})",
    CompareConditionInfo cc => $"{cc.FieldName} {FormatOp(cc.Op)} {cc.Value}",
    _                      => "?"
};

static string FormatOp(CompareOp op) => op switch
{
    CompareOp.Eq  => "==",
    CompareOp.Neq => "!=",
    CompareOp.Lt  => "<",
    CompareOp.Gt  => ">",
    CompareOp.Lte => "<=",
    CompareOp.Gte => ">=",
    _             => "?"
};
