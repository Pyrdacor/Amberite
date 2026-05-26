namespace Amber.Interpreter.Runtime;

using Amber.Interpreter.AST;

// ── Resolved field type references ───────────────────────────────────────────

public abstract record FieldTypeRef;
public record PrimitiveTypeRef(AmberType Type) : FieldTypeRef;
public record UserTypeRef(string TypeName) : FieldTypeRef;
public record PointerTypeRef(FieldTypeRef ElementType) : FieldTypeRef;

// ── Type info stored after a declaration is processed ────────────────────────

public record ParamInfo(
    ParamDirection Direction,
    string Register,
    FieldTypeRef Type,
    string Name);

public abstract record UserTypeInfo(string Name);

public record EnumTypeInfo(
    string Name,
    AmberType BaseType,
    IReadOnlyList<(string Name, long Value)> Members) : UserTypeInfo(Name);

public record BitfieldTypeInfo(
    string Name,
    AmberType BaseType,
    IReadOnlyList<(string Name, long Value)> Members) : UserTypeInfo(Name);

public record StructTypeInfo(
    string Name,
    IReadOnlyList<StructFieldInfo> Fields) : UserTypeInfo(Name);

public record FunctionInfo(
    string Name,
    IReadOnlyList<ParamInfo> Parameters,
    // Body stored as AST for future interpretation
    IReadOnlyList<StatementNode> Body) : UserTypeInfo(Name);

public record StructFieldInfo(FieldTypeRef Type, string Name, long? ArraySize);

// ── Registry ─────────────────────────────────────────────────────────────────

public class TypeRegistry
{
    private readonly Dictionary<string, UserTypeInfo> _types = new();

    public void Register(UserTypeInfo info)
    {
        if (_types.ContainsKey(info.Name))
            throw new AmberRuntimeException($"Type '{info.Name}' is already defined");
        _types[info.Name] = info;
    }

    public UserTypeInfo Get(string name)
    {
        if (!_types.TryGetValue(name, out var info))
            throw new AmberRuntimeException($"Unknown type '{name}'");
        return info;
    }

    public T Get<T>(string name) where T : UserTypeInfo
    {
        var info = Get(name);
        if (info is not T typed)
            throw new AmberRuntimeException(
                $"'{name}' is a {info.GetType().Name} not a {typeof(T).Name}");
        return typed;
    }

    public bool Contains(string name) => _types.ContainsKey(name);

    public IEnumerable<UserTypeInfo> All() => _types.Values;
}
