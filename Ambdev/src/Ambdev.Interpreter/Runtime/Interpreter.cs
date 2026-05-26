namespace Ambdev.Interpreter.Runtime;

using Ambdev.Interpreter.AST;

public class AmbdevInterpreter
{
    public AmbdevRegistry Registry { get; } = new();

    public void Execute(ProgramNode program)
    {
        // Phase 1: register everything except chains
        var pendingChains = new List<ChainDeclarationNode>();
        foreach (var item in program.Items)
        {
            switch (item)
            {
                case ConstDeclarationNode co:   RegisterConst(co);   break;
                case EnumDeclarationNode e:      RegisterEnum(e);     break;
                case BitfieldDeclarationNode b:  RegisterBitfield(b); break;
                case EtypeDeclarationNode et:    RegisterEtype(et);   break;
                case EspecDeclarationNode es:    RegisterEspec(es);   break;
                case EventDeclarationNode ev:    RegisterEvent(ev);   break;
                case ChainDeclarationNode c:     pendingChains.Add(c); break;
                default:
                    throw new AmbdevRuntimeException(
                        $"Unhandled top-level item: {item.GetType().Name}");
            }
        }

        // Phase 2: register chains with name→index maps so forward references work
        var eventByName = Registry.Events.Values.ToDictionary(e => e.Name, e => e.Index);
        var chainByName = new Dictionary<string, int>();
        foreach (var c in pendingChains)
            chainByName.TryAdd(c.Name, c.Index);

        foreach (var c in pendingChains)
            RegisterChain(c, eventByName, chainByName);
    }

    // ── Constants ─────────────────────────────────────────────────────────────

    private void RegisterConst(ConstDeclarationNode decl)
    {
        if (decl.Name.EndsWith('_'))
            throw new AmbdevRuntimeException(
                $"Constant name '{decl.Name}' must not end with an underscore");
        var value = EvaluateConstExpr(decl.Expr, $"const '{decl.Name}'");
        Registry.RegisterConstant(new ConstantInfo(decl.Name, value));
    }

    private long EvaluateConstExpr(ConstExprNode expr, string context) => expr switch
    {
        LiteralConstExprNode lit  => lit.Value,
        BoolConstExprNode    b    => b.Value ? 1L : 0L,
        ConstRefNode         r    => Registry.GetConstant(r.Name).Value,
        NegConstExprNode     neg  => -EvaluateConstExpr(neg.Operand, context),
        BinaryConstExprNode  bin  => EvaluateBinary(bin, context),
        _ => throw new AmbdevRuntimeException($"Unknown const expr in {context}: {expr.GetType().Name}")
    };

    private long EvaluateBinary(BinaryConstExprNode bin, string context)
    {
        var l = EvaluateConstExpr(bin.Left,  context);
        var r = EvaluateConstExpr(bin.Right, context);
        return bin.Op switch
        {
            BinaryOp.Add => l + r,
            BinaryOp.Sub => l - r,
            BinaryOp.Mul => l * r,
            BinaryOp.Div => r == 0
                ? throw new AmbdevRuntimeException($"Division by zero in {context}")
                : l / r,
            _ => throw new AmbdevRuntimeException($"Unknown binary op in {context}")
        };
    }

    // ── Enum / Bitfield ───────────────────────────────────────────────────────

    private void RegisterEnum(EnumDeclarationNode decl)
    {
        var members = new List<(string Name, long Value)>();
        long current = 0;
        foreach (var m in decl.Members)
        {
            long value = m.ValueExpr != null
                ? EvaluateConstExpr(m.ValueExpr, $"enum '{decl.Name}' member '{m.Name}'")
                : current;
            CheckFitsType(value, decl.BaseType, $"enum '{decl.Name}' member '{m.Name}'");
            members.Add((m.Name, value));
            current = value + 1;
        }
        Registry.RegisterEnum(new EnumInfo(decl.Name, decl.BaseType, members));
    }

    private void RegisterBitfield(BitfieldDeclarationNode decl)
    {
        var members    = new List<(string Name, long Value)>();
        long currentBit = 1;
        foreach (var m in decl.Members)
        {
            long value;
            if (m.ValueExpr != null)
            {
                value = ResolveBitfieldExpr(m.ValueExpr, members, decl.Name);
                currentBit = 1;
                while (currentBit <= value) currentBit <<= 1;
            }
            else
            {
                value = currentBit;
                currentBit <<= 1;
            }
            CheckFitsType(value, decl.BaseType, $"bitfield '{decl.Name}' member '{m.Name}'");
            members.Add((m.Name, value));
        }

        if (!members.Any(m => m.Value == 0) && !members.Any(m => m.Name == "None"))
            members.Insert(0, ("None", 0L));

        Registry.RegisterBitfield(new BitfieldInfo(decl.Name, decl.BaseType, members));
    }

    private long ResolveBitfieldExpr(
        BitfieldValueExprNode expr,
        List<(string Name, long Value)> defined,
        string bitfieldName) => expr switch
    {
        LiteralBitfieldValueNode  lit => lit.Value,
        IdentRefBitfieldValueNode r   => ResolveBitfieldIdentRef(r.Name, defined, bitfieldName),
        OrBitfieldValueNode       or  =>
            ResolveBitfieldExpr(or.Left, defined, bitfieldName) |
            ResolveBitfieldExpr(or.Right, defined, bitfieldName),
        _ => throw new AmbdevRuntimeException($"Unknown bitfield value expr: {expr.GetType().Name}")
    };

    private long ResolveBitfieldIdentRef(
        string name,
        List<(string Name, long Value)> defined,
        string bitfieldName)
    {
        var memberVal = defined.Where(m => m.Name == name).Select(m => (long?)m.Value).FirstOrDefault();
        if (memberVal.HasValue) return memberVal.Value;
        if (Registry.Constants.TryGetValue(name, out var c)) return c.Value;
        throw new AmbdevRuntimeException(
            $"Bitfield '{bitfieldName}': '{name}' is neither a defined member nor a known constant");
    }

    // ── Etype ─────────────────────────────────────────────────────────────────

    private void RegisterEtype(EtypeDeclarationNode decl)
    {
        var fields = decl.Fields.Select(BuildFieldInfo).ToList();
        Registry.RegisterEtype(new EtypeInfo(decl.Index, decl.Name, fields));
    }

    // ── Espec ─────────────────────────────────────────────────────────────────

    private void RegisterEspec(EspecDeclarationNode decl)
    {
        var etype      = Registry.GetEtype(decl.EtypeIndex);
        var conditions = new List<ConditionInfo>();
        var fields     = new List<FieldInfo>();

        foreach (var item in decl.Items)
        {
            switch (item)
            {
                case WhenEspecItemNode w:
                    var cond = ResolveCondition(w.Condition);
                    ValidateCondition(cond, etype, decl.Name);
                    conditions.Add(cond);
                    break;
                case EtypeFieldNode f:
                    fields.Add(BuildFieldInfo(f));
                    break;
            }
        }

        Registry.RegisterEspec(new EspecInfo(decl.EtypeIndex, decl.Name, conditions, fields));
    }

    private static ConditionInfo ResolveCondition(ConditionNode node) => node switch
    {
        AndConditionNode a     => new AndConditionInfo(ResolveCondition(a.Left), ResolveCondition(a.Right)),
        OrConditionNode  o     => new OrConditionInfo(ResolveCondition(o.Left), ResolveCondition(o.Right)),
        CompareConditionNode c => new CompareConditionInfo(c.FieldName, c.Op, c.Value),
        _ => throw new AmbdevRuntimeException($"Unknown condition node: {node.GetType().Name}")
    };

    private static void ValidateCondition(ConditionInfo cond, EtypeInfo etype, string especName)
    {
        switch (cond)
        {
            case AndConditionInfo a:
                ValidateCondition(a.Left,  etype, especName);
                ValidateCondition(a.Right, etype, especName);
                break;
            case OrConditionInfo o:
                ValidateCondition(o.Left,  etype, especName);
                ValidateCondition(o.Right, etype, especName);
                break;
            case CompareConditionInfo c:
                if (!etype.Fields.Any(f => f.Name == c.FieldName))
                    throw new AmbdevRuntimeException(
                        $"Espec '{especName}': condition references unknown field '{c.FieldName}' in etype {etype.Index}");
                break;
        }
    }

    // ── Event ─────────────────────────────────────────────────────────────────

    private void RegisterEvent(EventDeclarationNode decl)
    {
        var etype  = Registry.GetEtype(decl.EtypeIndex);
        var fields = decl.Fields
            .Select(f => new EventFieldValue(f.FieldName, ResolveEventValueExpr(f.ValueExpr, decl.Index)))
            .ToList();

        foreach (var ef in etype.Fields.Where(f => !f.IsOptional))
        {
            if (!fields.Any(f => f.Name == ef.Name))
                throw new AmbdevRuntimeException(
                    $"Event {decl.Index}: required field '{ef.Name}' is missing");
        }

        Registry.RegisterEvent(new EventInfo(decl.Index, decl.Name, decl.EtypeIndex, fields));
    }

    private long ResolveEventValueExpr(EventValueExprNode expr, int eventIndex) => expr switch
    {
        LiteralEventValueNode   lit => lit.Value,
        QualifiedEventValueNode q   => ResolveQualifiedMember(q.TypeName, q.MemberName, eventIndex),
        ConstRefEventValueNode  cr  => Registry.GetConstant(cr.ConstName).Value,
        OrEventValueNode        or  => ResolveEventValueExpr(or.Left, eventIndex)
                                    | ResolveEventValueExpr(or.Right, eventIndex),
        _ => throw new AmbdevRuntimeException($"Unknown event value expr: {expr.GetType().Name}")
    };

    private long ResolveQualifiedMember(string typeName, string memberName, int eventIndex)
    {
        if (Registry.Enums.TryGetValue(typeName, out var enumInfo))
        {
            var m = enumInfo.Members.FirstOrDefault(x => x.Name == memberName);
            if (m.Name == null)
                throw new AmbdevRuntimeException(
                    $"Event {eventIndex}: enum '{typeName}' has no member '{memberName}'");
            return m.Value;
        }
        if (Registry.Bitfields.TryGetValue(typeName, out var bfInfo))
        {
            var m = bfInfo.Members.FirstOrDefault(x => x.Name == memberName);
            if (m.Name == null)
                throw new AmbdevRuntimeException(
                    $"Event {eventIndex}: bitfield '{typeName}' has no member '{memberName}'");
            return m.Value;
        }
        throw new AmbdevRuntimeException(
            $"Event {eventIndex}: unknown type '{typeName}' in '{typeName}.{memberName}'");
    }

    // ── Chain ─────────────────────────────────────────────────────────────────

    private void RegisterChain(
        ChainDeclarationNode decl,
        Dictionary<string, int> eventByName,
        Dictionary<string, int> chainByName)
    {
        var steps = decl.Steps.Select(s =>
        {
            int index;
            if (s.TargetIndex.HasValue)
            {
                index = s.TargetIndex.Value;
            }
            else
            {
                var name = s.TargetName!;
                var map  = s.Target == StepTarget.Event ? eventByName : chainByName;
                if (!map.TryGetValue(name, out index))
                    throw new AmbdevRuntimeException(
                        $"Chain '{decl.Name}': unknown {(s.Target == StepTarget.Event ? "event" : "chain")} '{name}'");
            }
            return new ChainStep(s.Prefix, s.Target, index);
        }).ToList();
        Registry.RegisterChain(new ChainInfo(decl.Index, decl.Name, steps));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private FieldInfo BuildFieldInfo(EtypeFieldNode f)
    {
        long? defVal = null;
        if (f.DefaultValue != null)
        {
            defVal = EvaluateConstExpr(f.DefaultValue, $"field '{f.Name}' default");
            var typeRef = ResolveFieldType(f.Type);
            if (typeRef is PrimitiveTypeRef prim)
                CheckFitsType(defVal.Value, prim.Type, $"field '{f.Name}' default");
        }
        return new(f.Offset, ResolveFieldType(f.Type), f.IsOptional, f.Name, defVal, ResolveRange(f.Range));
    }

    private static void CheckFitsType(long value, PrimType type, string context)
    {
        long max = type switch
        {
            PrimType.Byte => 255L,
            PrimType.Word => 65535L,
            PrimType.Long => 4294967295L,
            _ => long.MaxValue
        };
        if (value < 0 || value > max)
            throw new AmbdevRuntimeException(
                $"{context}: value {value} does not fit in {type.ToString().ToLower()} (max {max})");
    }

    private static AmbdevTypeRef ResolveFieldType(FieldTypeNode node) => node switch
    {
        PrimitiveFieldTypeNode p => new PrimitiveTypeRef(p.Type),
        NamedFieldTypeNode     n => new NamedTypeRef(n.TypeName),
        _ => throw new AmbdevRuntimeException($"Unknown field type node: {node.GetType().Name}")
    };

    private static RangeConstraint? ResolveRange(RangeConstraintNode? node) => node switch
    {
        null                  => null,
        ContinuousRangeNode r => new ContinuousRange(r.Min, r.Max),
        ValueListRangeNode  v => new ValueListRange(v.Values),
        _ => throw new AmbdevRuntimeException($"Unknown range node: {node.GetType().Name}")
    };
}
