namespace Amber.Interpreter.Runtime;

using Amber.Interpreter.AST;

public class AmberInterpreter
{
    public TypeRegistry   Types       { get; } = new();
    public AmberEnvironment Environment { get; } = new();

    public void Execute(ProgramNode program)
    {
        foreach (var item in program.Items)
        {
            switch (item)
            {
                case TypeDeclarationNode typeDecl:
                    ExecuteTypeDecl(typeDecl);
                    break;
                case StatementNode stmt:
                    ExecuteStatement(stmt);
                    break;
                default:
                    throw new AmberRuntimeException(
                        $"Unhandled top-level item: {item.GetType().Name}");
            }
        }
    }

    // ── Type declarations ─────────────────────────────────────────────────────

    private void ExecuteTypeDecl(TypeDeclarationNode decl)
    {
        switch (decl)
        {
            case EnumDeclarationNode e:      RegisterEnum(e);     break;
            case BitfieldDeclarationNode b:  RegisterBitfield(b); break;
            case StructDeclarationNode s:    RegisterStruct(s);   break;
            case FunctionDeclarationNode f:  RegisterFunction(f); break;
            default:
                throw new AmberRuntimeException(
                    $"Unhandled type declaration: {decl.GetType().Name}");
        }
    }

    private void RegisterEnum(EnumDeclarationNode decl)
    {
        var members = new List<(string Name, long Value)>();
        long current = 0;
        foreach (var m in decl.Members)
        {
            long value = m.ExplicitValue ?? current;
            members.Add((m.Name, value));
            current = value + 1;
        }
        Types.Register(new EnumTypeInfo(decl.Name, decl.BaseType, members));
    }

    private void RegisterBitfield(BitfieldDeclarationNode decl)
    {
        var members    = new List<(string Name, long Value)>();
        long currentBit = 1;
        foreach (var m in decl.Members)
        {
            long value;
            if (m.ExplicitValue.HasValue)
            {
                value = m.ExplicitValue.Value;
                // Resume auto-increment from the next power of 2 above the explicit value
                currentBit = 1;
                while (currentBit <= value) currentBit <<= 1;
            }
            else
            {
                value = currentBit;
                currentBit <<= 1;
            }
            members.Add((m.Name, value));
        }
        Types.Register(new BitfieldTypeInfo(decl.Name, decl.BaseType, members));
    }

    private void RegisterStruct(StructDeclarationNode decl)
    {
        var fields = new List<StructFieldInfo>();
        foreach (var item in decl.Items)
        {
            switch (item)
            {
                case StructFieldNode f:
                    fields.Add(new StructFieldInfo(ResolveFieldType(f.Type), f.Name, f.ArraySize));
                    break;
                case StructImportNode imp:
                    // The imported struct is already fully resolved, so its Fields list
                    // is already flat — nested imports are handled for free.
                    fields.AddRange(Types.Get<StructTypeInfo>(imp.TypeName).Fields);
                    break;
            }
        }
        Types.Register(new StructTypeInfo(decl.Name, fields));
    }

    private void RegisterFunction(FunctionDeclarationNode decl)
    {
        foreach (var p in decl.Parameters)
            ValidateRegister(p.Register, decl.Name);

        var resolved = decl.Parameters
            .Select(p => new ParamInfo(p.Direction, p.Register, ResolveFieldType(p.Type), p.Name))
            .ToList();

        Types.Register(new FunctionInfo(decl.Name, resolved, decl.Body));
    }

    private static void ValidateRegister(string reg, string funcName)
    {
        bool isData = reg.Length == 2 && reg[0] == 'd' && reg[1] is >= '0' and <= '7';
        bool isAddr = reg.Length == 2 && reg[0] == 'a' && reg[1] is >= '0' and <= '6';
        if (!isData && !isAddr)
            throw new AmberRuntimeException(
                $"Invalid register '{reg}' in function '{funcName}'. " +
                $"Use d0–d7 for values or a0–a6 for pointers/structs.");
    }

    private FieldTypeRef ResolveFieldType(FieldTypeNode node) => node switch
    {
        PrimitiveFieldType p => new PrimitiveTypeRef(p.Type),
        NamedFieldType     n => new UserTypeRef(n.TypeName),
        PointerFieldType   p => new PointerTypeRef(ResolveFieldType(p.ElementType)),
        _ => throw new AmberRuntimeException($"Unknown field type node: {node.GetType().Name}")
    };

    // ── Statements ────────────────────────────────────────────────────────────

    private void ExecuteStatement(StatementNode stmt)
    {
        switch (stmt)
        {
            case VarDeclarationNode decl:
                var init = decl.Initializer is not null
                    ? EvalExpr(decl.Initializer)
                    : (AmberValue?)null;
                Environment.Declare(decl.Name, decl.Type, init);
                break;

            case AssignmentNode assign:
                Environment.Set(assign.Name, EvalExpr(assign.Value));
                break;

            default:
                throw new AmberRuntimeException(
                    $"Unhandled statement type: {stmt.GetType().Name}");
        }
    }

    // ── Expressions ──────────────────────────────────────────────────────────

    private AmberValue EvalExpr(ExpressionNode expr) => expr switch
    {
        IntLiteralNode  lit    => new AmberValue(AmberType.Long, lit.Value),
        BoolLiteralNode lit    => AmberValue.FromBool(lit.Value),
        IdentifierNode  id     => Environment.Get(id.Name),
        PartAccessNode  part   => EvalPartAccess(part),
        UnaryExprNode   unary  => EvalUnary(unary),
        BinaryExprNode  binary => EvalBinary(binary),
        _ => throw new AmberRuntimeException(
            $"Unhandled expression type: {expr.GetType().Name}")
    };

    private AmberValue EvalPartAccess(PartAccessNode n)
    {
        var v = EvalExpr(n.Source);
        return (v.Type, n.Part) switch
        {
            // long sub-parts
            (AmberType.Long, "w0") => AmberValue.FromWord( v.RawValue         & 0xFFFF),
            (AmberType.Long, "w1") => AmberValue.FromWord((v.RawValue >> 16)  & 0xFFFF),
            (AmberType.Long, "b0") => AmberValue.FromByte( v.RawValue         & 0xFF),
            (AmberType.Long, "b1") => AmberValue.FromByte((v.RawValue >>  8)  & 0xFF),
            (AmberType.Long, "b2") => AmberValue.FromByte((v.RawValue >> 16)  & 0xFF),
            (AmberType.Long, "b3") => AmberValue.FromByte((v.RawValue >> 24)  & 0xFF),
            // word sub-parts
            (AmberType.Word, "b0") => AmberValue.FromByte( v.RawValue         & 0xFF),
            (AmberType.Word, "b1") => AmberValue.FromByte((v.RawValue >>  8)  & 0xFF),
            _ => throw new AmberRuntimeException(
                $"Type '{v.Type.ToString().ToLower()}' has no sub-part '{n.Part}'")
        };
    }

    private AmberValue EvalUnary(UnaryExprNode u)
    {
        var v = EvalExpr(u.Operand);
        return u.Op switch
        {
            UnaryOp.Negate     => new AmberValue(v.Type, -v.RawValue).Coerce(v.Type),
            UnaryOp.BitwiseNot => new AmberValue(v.Type, ~v.RawValue).Coerce(v.Type),
            UnaryOp.LogicalNot => AmberValue.FromBool(v.RawValue == 0),
            _ => throw new AmberRuntimeException($"Unknown unary op: {u.Op}")
        };
    }

    private AmberValue EvalBinary(BinaryExprNode b)
    {
        var left  = EvalExpr(b.Left);
        var right = EvalExpr(b.Right);
        var resultType = Promote(left.Type, right.Type);
        long lv = left.RawValue, rv = right.RawValue;

        long raw = b.Op switch
        {
            BinaryOp.Add => lv + rv,
            BinaryOp.Sub => lv - rv,
            BinaryOp.Mul => lv * rv,
            BinaryOp.Div => rv != 0 ? lv / rv
                : throw new AmberRuntimeException("Division by zero"),
            BinaryOp.Mod => rv != 0 ? lv % rv
                : throw new AmberRuntimeException("Modulo by zero"),
            BinaryOp.And => lv & rv,
            BinaryOp.Or  => lv | rv,
            BinaryOp.Xor => lv ^ rv,
            BinaryOp.Shl => lv << (int)(rv & 63),
            BinaryOp.Shr => lv >> (int)(rv & 63),
            _ => throw new AmberRuntimeException($"Unknown binary op: {b.Op}")
        };

        return new AmberValue(resultType, raw).Coerce(resultType);
    }

    private static AmberType Promote(AmberType a, AmberType b)
    {
        if (a == AmberType.Bool && b == AmberType.Bool) return AmberType.Bool;
        if (a == AmberType.Bool) return b;
        if (b == AmberType.Bool) return a;
        if (a == AmberType.Long || b == AmberType.Long) return AmberType.Long;
        if (a == AmberType.Word || b == AmberType.Word) return AmberType.Word;
        return AmberType.Byte;
    }
}
