namespace Amber.Interpreter.Runtime;

using Amber.Interpreter.AST;

public class AmberEnvironment
{
    private readonly Dictionary<string, (AmberType Type, AmberValue Value)> _vars = new();

    public void Declare(string name, AmberType type, AmberValue? initializer = null)
    {
        if (_vars.ContainsKey(name))
            throw new AmberRuntimeException($"Variable '{name}' is already declared");
        _vars[name] = (type, initializer?.Coerce(type) ?? AmberValue.Default(type));
    }

    public AmberValue Get(string name)
    {
        if (!_vars.TryGetValue(name, out var entry))
            throw new AmberRuntimeException($"Undefined variable '{name}'");
        return entry.Value;
    }

    public void Set(string name, AmberValue value)
    {
        if (!_vars.TryGetValue(name, out var entry))
            throw new AmberRuntimeException($"Undefined variable '{name}'");
        _vars[name] = (entry.Type, value.Coerce(entry.Type));
    }

    public IEnumerable<(string Name, AmberType Type, AmberValue Value)> All()
        => _vars.Select(kv => (kv.Key, kv.Value.Type, kv.Value.Value));
}
