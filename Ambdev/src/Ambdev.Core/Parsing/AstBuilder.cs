namespace Ambdev.Interpreter.Parsing;

using Ambdev.Interpreter.AST;
using Ambdev.Interpreter.Grammar;

public sealed class AstBuilder : AmbdevBaseVisitor<AstNode>
{
    public ProgramNode BuildProgram(AmbdevParser.ProgramContext ctx)
        => (ProgramNode)Visit(ctx);

    // ── Top level ─────────────────────────────────────────────────────────────

    public override AstNode VisitProgram(AmbdevParser.ProgramContext ctx)
        => new ProgramNode(ctx.topLevel().Select(t => Visit(t)).ToList());

    public override AstNode VisitTopLevel(AmbdevParser.TopLevelContext ctx)
    {
        if (ctx.enumDecl()     is { } e)  return Visit(e);
        if (ctx.bitfieldDecl() is { } b)  return Visit(b);
        if (ctx.constDecl()    is { } co) return Visit(co);
        if (ctx.etypeDecl()    is { } et) return Visit(et);
        if (ctx.especDecl()    is { } es) return Visit(es);
        if (ctx.eventDecl()    is { } ev) return Visit(ev);
        if (ctx.chainDecl()    is { } c)  return Visit(c);
        throw new InvalidOperationException("Empty topLevel context");
    }

    // ── Constants ─────────────────────────────────────────────────────────────

    public override AstNode VisitConstDecl(AmbdevParser.ConstDeclContext ctx)
        => new ConstDeclarationNode(GetName(ctx.ident()), (ConstExprNode)Visit(ctx.constExpr()));

    public override AstNode VisitMulConstExpr(AmbdevParser.MulConstExprContext ctx)
        => new BinaryConstExprNode((ConstExprNode)Visit(ctx.constExpr(0)), BinaryOp.Mul, (ConstExprNode)Visit(ctx.constExpr(1)));

    public override AstNode VisitDivConstExpr(AmbdevParser.DivConstExprContext ctx)
        => new BinaryConstExprNode((ConstExprNode)Visit(ctx.constExpr(0)), BinaryOp.Div, (ConstExprNode)Visit(ctx.constExpr(1)));

    public override AstNode VisitAddConstExpr(AmbdevParser.AddConstExprContext ctx)
        => new BinaryConstExprNode((ConstExprNode)Visit(ctx.constExpr(0)), BinaryOp.Add, (ConstExprNode)Visit(ctx.constExpr(1)));

    public override AstNode VisitSubConstExpr(AmbdevParser.SubConstExprContext ctx)
        => new BinaryConstExprNode((ConstExprNode)Visit(ctx.constExpr(0)), BinaryOp.Sub, (ConstExprNode)Visit(ctx.constExpr(1)));

    public override AstNode VisitNegConstExpr(AmbdevParser.NegConstExprContext ctx)
        => new NegConstExprNode((ConstExprNode)Visit(ctx.constExpr()));

    public override AstNode VisitParenConstExpr(AmbdevParser.ParenConstExprContext ctx)
        => Visit(ctx.constExpr());

    public override AstNode VisitTrueConstExpr(AmbdevParser.TrueConstExprContext ctx)
        => new BoolConstExprNode(true);

    public override AstNode VisitFalseConstExpr(AmbdevParser.FalseConstExprContext ctx)
        => new BoolConstExprNode(false);

    public override AstNode VisitNumLiteralConstExpr(AmbdevParser.NumLiteralConstExprContext ctx)
        => new LiteralConstExprNode(ParseLiteral(ctx.literal()));

    public override AstNode VisitConstRefConstExpr(AmbdevParser.ConstRefConstExprContext ctx)
        => new ConstRefNode(GetName(ctx.ident()));

    // ── Enum ──────────────────────────────────────────────────────────────────

    public override AstNode VisitEnumDecl(AmbdevParser.EnumDeclContext ctx)
    {
        var name     = GetName(ctx.ident());
        var baseType = ParseBaseType(ctx.baseType());
        var members  = ctx.enumMembers().enumMember()
            .Select(m =>
            {
                var mName   = GetName(m.ident());
                var valExpr = m.constExpr() is { } vCtx ? (ConstExprNode)Visit(vCtx) : null;
                return new EnumMemberNode(mName, valExpr);
            })
            .ToList();
        return new EnumDeclarationNode(name, baseType, members);
    }

    // ── Bitfield ──────────────────────────────────────────────────────────────

    public override AstNode VisitBitfieldDecl(AmbdevParser.BitfieldDeclContext ctx)
    {
        var name     = GetName(ctx.ident());
        var baseType = ParseBaseType(ctx.baseType());
        var members  = ctx.bitfieldMembers().bitfieldMember()
            .Select(m =>
            {
                var mName   = GetName(m.ident());
                var valExpr = m.bitfieldValueExpr() is { } vCtx
                    ? (BitfieldValueExprNode)Visit(vCtx)
                    : null;
                return new BitfieldMemberNode(mName, valExpr);
            })
            .ToList();
        return new BitfieldDeclarationNode(name, baseType, members);
    }

    public override AstNode VisitOrBitfieldValue(AmbdevParser.OrBitfieldValueContext ctx)
        => new OrBitfieldValueNode(
            (BitfieldValueExprNode)Visit(ctx.bitfieldValueExpr(0)),
            (BitfieldValueExprNode)Visit(ctx.bitfieldValueExpr(1)));

    public override AstNode VisitLiteralBitfieldValue(AmbdevParser.LiteralBitfieldValueContext ctx)
        => new LiteralBitfieldValueNode(ParseLiteral(ctx.literal()));

    public override AstNode VisitIdentRefBitfieldValue(AmbdevParser.IdentRefBitfieldValueContext ctx)
        => new IdentRefBitfieldValueNode(GetName(ctx.ident()));

    // ── Field type nodes ──────────────────────────────────────────────────────

    public override AstNode VisitByteFieldType(AmbdevParser.ByteFieldTypeContext ctx)
        => new PrimitiveFieldTypeNode(PrimType.Byte);

    public override AstNode VisitWordFieldType(AmbdevParser.WordFieldTypeContext ctx)
        => new PrimitiveFieldTypeNode(PrimType.Word);

    public override AstNode VisitLongFieldType(AmbdevParser.LongFieldTypeContext ctx)
        => new PrimitiveFieldTypeNode(PrimType.Long);

    public override AstNode VisitNamedFieldType(AmbdevParser.NamedFieldTypeContext ctx)
        => new NamedFieldTypeNode(GetName(ctx.ident()));

    // ── Etype ─────────────────────────────────────────────────────────────────

    public override AstNode VisitEtypeDecl(AmbdevParser.EtypeDeclContext ctx)
    {
        var index  = int.Parse(ctx.INTEGER_LITERAL().GetText());
        var name   = GetName(ctx.ident());
        var fields = ctx.etypeField().Select(f => (EtypeFieldNode)Visit(f)).ToList();
        return new EtypeDeclarationNode(index, name, fields);
    }

    public override AstNode VisitEtypeField(AmbdevParser.EtypeFieldContext ctx)
        => BuildFieldNode(ctx.INTEGER_LITERAL().GetText(), ctx.fieldType(),
            ctx.QUESTION(), GetName(ctx.ident()), ctx.constExpr(), ctx.rangeConstraint());

    // ── Espec ─────────────────────────────────────────────────────────────────

    public override AstNode VisitEspecDecl(AmbdevParser.EspecDeclContext ctx)
    {
        var index = int.Parse(ctx.INTEGER_LITERAL().GetText());
        var name  = GetName(ctx.ident());
        var items = ctx.especItem().Select(i => (EspecItemNode)Visit(i)).ToList();
        return new EspecDeclarationNode(index, name, items);
    }

    public override AstNode VisitWhenEspecItem(AmbdevParser.WhenEspecItemContext ctx)
        => new WhenEspecItemNode((ConditionNode)Visit(ctx.condition()));

    public override AstNode VisitFieldEspecItem(AmbdevParser.FieldEspecItemContext ctx)
        => BuildFieldNode(ctx.INTEGER_LITERAL().GetText(), ctx.fieldType(),
            ctx.QUESTION(), GetName(ctx.ident()), ctx.constExpr(), ctx.rangeConstraint());

    // ── Conditions ────────────────────────────────────────────────────────────

    public override AstNode VisitAndCondition(AmbdevParser.AndConditionContext ctx)
        => new AndConditionNode(
            (ConditionNode)Visit(ctx.left),
            (ConditionNode)Visit(ctx.right));

    public override AstNode VisitOrCondition(AmbdevParser.OrConditionContext ctx)
        => new OrConditionNode(
            (ConditionNode)Visit(ctx.left),
            (ConditionNode)Visit(ctx.right));

    public override AstNode VisitCompareCondition(AmbdevParser.CompareConditionContext ctx)
        => new CompareConditionNode(
            GetName(ctx.ident()),
            ParseCompareOp(ctx.compareOp()),
            ParseLiteral(ctx.literal()));

    // ── Range constraints ─────────────────────────────────────────────────────

    public override AstNode VisitContinuousRange(AmbdevParser.ContinuousRangeContext ctx)
        => new ContinuousRangeNode(
            long.Parse(ctx.INTEGER_LITERAL(0).GetText()),
            long.Parse(ctx.INTEGER_LITERAL(1).GetText()));

    public override AstNode VisitValueListRange(AmbdevParser.ValueListRangeContext ctx)
        => new ValueListRangeNode(
            ctx.INTEGER_LITERAL().Select(t => long.Parse(t.GetText())).ToList());

    // ── Event ─────────────────────────────────────────────────────────────────

    public override AstNode VisitEventDecl(AmbdevParser.EventDeclContext ctx)
    {
        var index    = int.Parse(ctx.INTEGER_LITERAL().GetText());
        var name     = GetName(ctx.ident());
        var etypeRef = (EtypeRefNode)Visit(ctx.etypeRef());
        var fields   = ctx.eventField().Select(f => (EventFieldNode)Visit(f)).ToList();
        return new EventDeclarationNode(index, name, etypeRef, fields);
    }

    public override AstNode VisitIndexEtypeRef(AmbdevParser.IndexEtypeRefContext ctx)
        => new IndexEtypeRefNode(int.Parse(ctx.INTEGER_LITERAL().GetText()));

    public override AstNode VisitNameEtypeRef(AmbdevParser.NameEtypeRefContext ctx)
        => new NameEtypeRefNode(GetName(ctx.ident()));

    public override AstNode VisitEventField(AmbdevParser.EventFieldContext ctx)
        => new EventFieldNode(GetName(ctx.ident()), (EventValueExprNode)Visit(ctx.eventValueExpr()));

    public override AstNode VisitOrEventValue(AmbdevParser.OrEventValueContext ctx)
        => new OrEventValueNode(
            (EventValueExprNode)Visit(ctx.eventValueExpr(0)),
            (EventValueExprNode)Visit(ctx.eventValueExpr(1)));

    public override AstNode VisitQualifiedEventValue(AmbdevParser.QualifiedEventValueContext ctx)
        => new QualifiedEventValueNode(GetName(ctx.ident(0)), GetName(ctx.ident(1)));

    public override AstNode VisitConstRefEventValue(AmbdevParser.ConstRefEventValueContext ctx)
        => new ConstRefEventValueNode(GetName(ctx.ident()));

    public override AstNode VisitLiteralEventValue(AmbdevParser.LiteralEventValueContext ctx)
        => new LiteralEventValueNode(ParseLiteral(ctx.literal()));

    // ── Chain ─────────────────────────────────────────────────────────────────

    public override AstNode VisitChainDecl(AmbdevParser.ChainDeclContext ctx)
    {
        var index = int.Parse(ctx.INTEGER_LITERAL().GetText());
        var name  = GetName(ctx.ident());
        var steps = ctx.chainStep().Select(s => (ChainStepNode)Visit(s)).ToList();
        return new ChainDeclarationNode(index, name, steps);
    }

    public override AstNode VisitChainStep(AmbdevParser.ChainStepContext ctx)
    {
        var prefix = ParseStepPrefix(ctx.stepPrefix());
        var target = ParseStepTarget(ctx.stepTarget());
        return ctx.chainStepTarget() switch
        {
            AmbdevParser.IndexChainTargetContext ict =>
                new ChainStepNode(prefix, target, int.Parse(ict.INTEGER_LITERAL().GetText()), null),
            AmbdevParser.NameChainTargetContext nct =>
                new ChainStepNode(prefix, target, null, GetName(nct.ident())),
            _ => throw new InvalidOperationException("Unknown chain step target")
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Shared field-node builder for etypeField and fieldEspecItem (identical structure)
    private EtypeFieldNode BuildFieldNode(
        string offsetText,
        AmbdevParser.FieldTypeContext typeCtx,
        Antlr4.Runtime.Tree.ITerminalNode? question,
        string name,
        AmbdevParser.ConstExprContext? constExprCtx,
        AmbdevParser.RangeConstraintContext? rangeCtx)
    {
        var offset     = int.Parse(offsetText);
        var type       = (FieldTypeNode)Visit(typeCtx);
        var isOptional = question != null;
        var defVal     = constExprCtx is { } ce ? (ConstExprNode)Visit(ce)       : null;
        var range      = rangeCtx     is { } rc ? (RangeConstraintNode)Visit(rc) : null;
        return new EtypeFieldNode(offset, type, isOptional, name, defVal, range);
    }

    private static string GetName(AmbdevParser.IdentContext ctx)
        => ctx.IDENTIFIER()?.GetText() ?? ctx.CONST_IDENTIFIER()!.GetText();

    private static PrimType ParseBaseType(AmbdevParser.BaseTypeContext ctx)
    {
        if (ctx.BYTE() != null) return PrimType.Byte;
        if (ctx.WORD() != null) return PrimType.Word;
        if (ctx.LONG() != null) return PrimType.Long;
        throw new InvalidOperationException($"Unknown base type '{ctx.GetText()}'");
    }

    private static CompareOp ParseCompareOp(AmbdevParser.CompareOpContext ctx)
    {
        if (ctx.EQ()  != null) return CompareOp.Eq;
        if (ctx.NEQ() != null) return CompareOp.Neq;
        if (ctx.LT()  != null) return CompareOp.Lt;
        if (ctx.GT()  != null) return CompareOp.Gt;
        if (ctx.LTE() != null) return CompareOp.Lte;
        if (ctx.GTE() != null) return CompareOp.Gte;
        throw new InvalidOperationException($"Unknown compare op '{ctx.GetText()}'");
    }

    private static StepPrefix ParseStepPrefix(AmbdevParser.StepPrefixContext ctx)
    {
        if (ctx.MINUS()    != null) return StepPrefix.Always;
        if (ctx.QUESTION() != null) return StepPrefix.IfSuccess;
        if (ctx.BANG()     != null) return StepPrefix.IfFailure;
        throw new InvalidOperationException($"Unknown step prefix '{ctx.GetText()}'");
    }

    private static StepTarget ParseStepTarget(AmbdevParser.StepTargetContext ctx)
    {
        if (ctx.EVENT() != null) return StepTarget.Event;
        if (ctx.CHAIN() != null) return StepTarget.Chain;
        throw new InvalidOperationException($"Unknown step target '{ctx.GetText()}'");
    }

    private static long ParseLiteral(AmbdevParser.LiteralContext ctx)
    {
        if (ctx.INTEGER_LITERAL() != null) return long.Parse(ctx.INTEGER_LITERAL().GetText());
        if (ctx.HEX_LITERAL()     != null) return Convert.ToInt64(ctx.HEX_LITERAL().GetText()[2..], 16);
        throw new InvalidOperationException($"Unknown literal '{ctx.GetText()}'");
    }
}
