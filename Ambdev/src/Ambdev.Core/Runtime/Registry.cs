namespace Ambdev.Interpreter.Runtime;

using Ambdev.Interpreter.AST;

// ── Field type references ─────────────────────────────────────────────────────

public abstract record AmbdevTypeRef;
public record PrimitiveTypeRef(PrimType Type) : AmbdevTypeRef;
public record NamedTypeRef(string TypeName) : AmbdevTypeRef;

// ── Range constraints ─────────────────────────────────────────────────────────

public abstract record RangeConstraint;
public record ContinuousRange(long Min, long Max) : RangeConstraint;
public record ValueListRange(IReadOnlyList<long> Values) : RangeConstraint;

// ── Constants ─────────────────────────────────────────────────────────────────

public record ConstantInfo(string Name, long Value);

// ── Enum / Bitfield ───────────────────────────────────────────────────────────

public record EnumInfo(
    string Name,
    PrimType BaseType,
    IReadOnlyList<(string Name, long Value)> Members);

public record BitfieldInfo(
    string Name,
    PrimType BaseType,
    IReadOnlyList<(string Name, long Value)> Members);

// ── Shared field info (used by etype and espec) ───────────────────────────────

public record FieldInfo(
    int Offset,
    AmbdevTypeRef Type,
    bool IsOptional,
    string Name,
    long? DefaultValue,
    RangeConstraint? Range);

// ── Etype ─────────────────────────────────────────────────────────────────────

public record EtypeInfo(
    int Index,
    string Name,
    IReadOnlyList<FieldInfo> Fields);

// ── Espec ─────────────────────────────────────────────────────────────────────

public abstract record ConditionInfo;
public record AndConditionInfo(ConditionInfo Left, ConditionInfo Right) : ConditionInfo;
public record OrConditionInfo(ConditionInfo Left, ConditionInfo Right) : ConditionInfo;
public record CompareConditionInfo(string FieldName, CompareOp Op, long Value) : ConditionInfo;

public record EspecInfo(
    int EtypeIndex,
    string Name,
    IReadOnlyList<ConditionInfo> Conditions,
    IReadOnlyList<FieldInfo> Fields);

// ── Event ─────────────────────────────────────────────────────────────────────

public record EventFieldValue(string Name, long Value);

public record EventInfo(
    int Index,
    string Name,
    int EtypeIndex,
    IReadOnlyList<EventFieldValue> Fields);

// ── Chain ─────────────────────────────────────────────────────────────────────

public record ChainStep(StepPrefix Prefix, StepTarget Target, int TargetIndex);

public record ChainInfo(
    int Index,
    string Name,
    IReadOnlyList<ChainStep> Steps);

// ── Registry ─────────────────────────────────────────────────────────────────

public class AmbdevRegistry
{
    private readonly Dictionary<string, ConstantInfo>  _constants   = new();
    private readonly Dictionary<string, EnumInfo>      _enums       = new();
    private readonly Dictionary<string, BitfieldInfo>  _bitfields   = new();
    private readonly Dictionary<int,    EtypeInfo>     _etypes      = new();
    private readonly Dictionary<string, int>           _etypeByName = new();
    private readonly Dictionary<string, EspecInfo>     _especs      = new();
    private readonly Dictionary<int,    EventInfo>     _events      = new();
    private readonly Dictionary<int,    ChainInfo>     _chains      = new();

    public IReadOnlyDictionary<string, ConstantInfo>  Constants  => _constants;
    public IReadOnlyDictionary<string, EnumInfo>      Enums      => _enums;
    public IReadOnlyDictionary<string, BitfieldInfo>  Bitfields  => _bitfields;
    public IReadOnlyDictionary<int,    EtypeInfo>     Etypes     => _etypes;
    public IReadOnlyDictionary<string, EspecInfo>     Especs     => _especs;
    public IReadOnlyDictionary<int,    EventInfo>     Events     => _events;
    public IReadOnlyDictionary<int,    ChainInfo>     Chains     => _chains;

    public void RegisterConstant(ConstantInfo info)
    {
        if (_constants.ContainsKey(info.Name))
            throw new AmbdevRuntimeException($"Duplicate constant '{info.Name}'");
        _constants[info.Name] = info;
    }

    public ConstantInfo GetConstant(string name)
        => _constants.TryGetValue(name, out var c) ? c
           : throw new AmbdevRuntimeException($"Unknown constant '{name}'");

    public void RegisterEnum(EnumInfo info)
    {
        if (_enums.ContainsKey(info.Name))
            throw new AmbdevRuntimeException($"Duplicate enum '{info.Name}'");
        _enums[info.Name] = info;
    }

    public void RegisterBitfield(BitfieldInfo info)
    {
        if (_bitfields.ContainsKey(info.Name))
            throw new AmbdevRuntimeException($"Duplicate bitfield '{info.Name}'");
        _bitfields[info.Name] = info;
    }

    public void RegisterEtype(EtypeInfo etype)
    {
        if (_etypes.ContainsKey(etype.Index))
            throw new AmbdevRuntimeException($"Duplicate etype index {etype.Index}");
        if (_etypeByName.ContainsKey(etype.Name))
            throw new AmbdevRuntimeException($"Duplicate etype name '{etype.Name}'");
        _etypes[etype.Index]      = etype;
        _etypeByName[etype.Name]  = etype.Index;
    }

    public void RegisterEspec(EspecInfo espec)
    {
        if (_especs.ContainsKey(espec.Name))
            throw new AmbdevRuntimeException($"Duplicate espec '{espec.Name}'");
        _especs[espec.Name] = espec;
    }

    public void RegisterEvent(EventInfo ev)
    {
        if (_events.ContainsKey(ev.Index))
            throw new AmbdevRuntimeException($"Duplicate event index {ev.Index}");
        _events[ev.Index] = ev;
    }

    public void RegisterChain(ChainInfo chain)
    {
        if (_chains.ContainsKey(chain.Index))
            throw new AmbdevRuntimeException($"Duplicate chain index {chain.Index}");
        _chains[chain.Index] = chain;
    }

    public EnumInfo GetEnum(string name)
        => _enums.TryGetValue(name, out var e) ? e
           : throw new AmbdevRuntimeException($"Unknown enum '{name}'");

    public BitfieldInfo GetBitfield(string name)
        => _bitfields.TryGetValue(name, out var b) ? b
           : throw new AmbdevRuntimeException($"Unknown bitfield '{name}'");

    public EtypeInfo GetEtype(int index)
        => _etypes.TryGetValue(index, out var e) ? e
           : throw new AmbdevRuntimeException($"Unknown etype index {index}");

    public EtypeInfo GetEtypeByName(string name)
        => _etypeByName.TryGetValue(name, out var idx)
           ? _etypes[idx]
           : throw new AmbdevRuntimeException($"Unknown etype name '{name}'");

    public EspecInfo GetEspec(string name)
        => _especs.TryGetValue(name, out var e) ? e
           : throw new AmbdevRuntimeException($"Unknown espec '{name}'");

    public EventInfo GetEvent(int index)
        => _events.TryGetValue(index, out var e) ? e
           : throw new AmbdevRuntimeException($"Unknown event index {index}");

    public ChainInfo GetChain(int index)
        => _chains.TryGetValue(index, out var c) ? c
           : throw new AmbdevRuntimeException($"Unknown chain index {index}");
}
