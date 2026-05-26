namespace Amber.Interpreter.Parsing;

using Amber.Interpreter.AST;
using Amber.Interpreter.Grammar;

/// Converts an ANTLR4 parse tree into a typed AST.
public sealed class AstBuilder : AmberBaseVisitor<AstNode>
{
    public ProgramNode BuildProgram(AmberParser.ProgramContext ctx)
        => (ProgramNode)Visit(ctx);

    // ── Top level ─────────────────────────────────────────────────────────────

    public override AstNode VisitProgram(AmberParser.ProgramContext ctx)
        => new ProgramNode(ctx.topLevel().Select(t => Visit(t)).ToList());

    public override AstNode VisitTopLevel(AmberParser.TopLevelContext ctx)
    {
        if (ctx.typeDeclaration() is { } td) return Visit(td);
        if (ctx.statement()       is { } st) return Visit(st);
        throw new InvalidOperationException("Empty topLevel context");
    }

    public override AstNode VisitTypeDeclaration(AmberParser.TypeDeclarationContext ctx)
    {
        if (ctx.enumDecl()     is { } e) return Visit(e);
        if (ctx.bitfieldDecl() is { } b) return Visit(b);
        if (ctx.structDecl()   is { } s) return Visit(s);
        if (ctx.functionDecl() is { } f) return Visit(f);
        throw new InvalidOperationException("Empty typeDeclaration context");
    }

    // ── Enum ──────────────────────────────────────────────────────────────────

    public override AstNode VisitEnumDecl(AmberParser.EnumDeclContext ctx)
    {
        var name     = ctx.IDENTIFIER().GetText();
        var baseType = ParseBaseType(ctx.baseType());
        var members  = ctx.enumMembers().enumMember()
            .Select(m =>
            {
                var mName  = m.IDENTIFIER().GetText();
                long? value = null;
                if (m.INTEGER_LITERAL() != null)
                    value = long.Parse(m.INTEGER_LITERAL().GetText());
                else if (m.HEX_LITERAL() != null)
                    value = Convert.ToInt64(m.HEX_LITERAL().GetText()[2..], 16);
                return new EnumMemberNode(mName, value);
            })
            .ToList();
        return new EnumDeclarationNode(name, baseType, members);
    }

    // ── Bitfield ──────────────────────────────────────────────────────────────

    public override AstNode VisitBitfieldDecl(AmberParser.BitfieldDeclContext ctx)
    {
        var members = ctx.bitfieldMembers().bitfieldMember()
            .Select(m =>
            {
                var mName  = m.IDENTIFIER().GetText();
                long? value = null;
                if (m.INTEGER_LITERAL() != null)
                    value = long.Parse(m.INTEGER_LITERAL().GetText());
                else if (m.HEX_LITERAL() != null)
                    value = Convert.ToInt64(m.HEX_LITERAL().GetText()[2..], 16);
                return new BitfieldMemberNode(mName, value);
            })
            .ToList();
        return new BitfieldDeclarationNode(
            ctx.IDENTIFIER().GetText(), ParseBaseType(ctx.baseType()), members);
    }

    // ── Struct ────────────────────────────────────────────────────────────────

    public override AstNode VisitStructDecl(AmberParser.StructDeclContext ctx)
    {
        var items = ctx.structItem()
            .Select(item =>
            {
                if (item.structField()  is { } f) return (StructBodyItem)Visit(f);
                if (item.structImport() is { } i) return (StructBodyItem)Visit(i);
                throw new InvalidOperationException("Empty structItem");
            })
            .ToList();
        return new StructDeclarationNode(ctx.IDENTIFIER().GetText(), items);
    }

    public override AstNode VisitStructField(AmberParser.StructFieldContext ctx)
    {
        var type = (FieldTypeNode)Visit(ctx.typeRef());
        var name = ctx.IDENTIFIER().GetText();
        long? size = ctx.INTEGER_LITERAL() != null
            ? long.Parse(ctx.INTEGER_LITERAL().GetText())
            : null;
        return new StructFieldNode(type, name, size);
    }

    public override AstNode VisitStructImport(AmberParser.StructImportContext ctx)
        => new StructImportNode(ctx.IDENTIFIER().GetText());

    // ── Function ──────────────────────────────────────────────────────────────

    public override AstNode VisitFunctionDecl(AmberParser.FunctionDeclContext ctx)
    {
        var name   = ctx.IDENTIFIER().GetText();
        var parms  = ctx.paramList()?.param()
                        .Select(p => (ParamNode)Visit(p))
                        .ToList()
                    ?? [];
        var body   = ctx.statement()
                        .Select(s => (StatementNode)Visit(s))
                        .ToList();
        return new FunctionDeclarationNode(name, parms, body);
    }

    public override AstNode VisitParam(AmberParser.ParamContext ctx)
    {
        var dir      = ParseParamDir(ctx.paramDir());
        var register = ctx.IDENTIFIER(0).GetText();
        var type     = (FieldTypeNode)Visit(ctx.typeRef());
        var name     = ctx.IDENTIFIER(1).GetText();
        return new ParamNode(dir, register, type, name);
    }

    private static ParamDirection ParseParamDir(AmberParser.ParamDirContext ctx)
    {
        if (ctx.IN()    != null) return ParamDirection.In;
        if (ctx.OUT()   != null) return ParamDirection.Out;
        if (ctx.INOUT() != null) return ParamDirection.InOut;
        throw new InvalidOperationException($"Unknown param direction '{ctx.GetText()}'");
    }

    // ── Type references (typeRef rule) ────────────────────────────────────────

    public override AstNode VisitPtrTypeRef(AmberParser.PtrTypeRefContext ctx)
        => new PointerFieldType((FieldTypeNode)Visit(ctx.typeRef()));

    public override AstNode VisitByteTypeRef(AmberParser.ByteTypeRefContext ctx)
        => new PrimitiveFieldType(AmberType.Byte);

    public override AstNode VisitWordTypeRef(AmberParser.WordTypeRefContext ctx)
        => new PrimitiveFieldType(AmberType.Word);

    public override AstNode VisitLongTypeRef(AmberParser.LongTypeRefContext ctx)
        => new PrimitiveFieldType(AmberType.Long);

    public override AstNode VisitBoolTypeRef(AmberParser.BoolTypeRefContext ctx)
        => new PrimitiveFieldType(AmberType.Bool);

    public override AstNode VisitNamedTypeRef(AmberParser.NamedTypeRefContext ctx)
        => new NamedFieldType(ctx.IDENTIFIER().GetText());

    // ── Statements ────────────────────────────────────────────────────────────

    public override AstNode VisitStatement(AmberParser.StatementContext ctx)
    {
        if (ctx.varDeclaration() is { } d) return Visit(d);
        if (ctx.assignment()     is { } a) return Visit(a);
        throw new InvalidOperationException("Empty statement context");
    }

    public override AstNode VisitVarDeclaration(AmberParser.VarDeclarationContext ctx)
    {
        var type = ParseType(ctx.typeSpec());
        var name = ctx.IDENTIFIER().GetText();
        var init = ctx.expression() is { } expr ? (ExpressionNode)Visit(expr) : null;
        return new VarDeclarationNode(type, name, init);
    }

    public override AstNode VisitAssignment(AmberParser.AssignmentContext ctx)
        => new AssignmentNode(ctx.IDENTIFIER().GetText(),
                              (ExpressionNode)Visit(ctx.expression()));

    // ── Expressions ──────────────────────────────────────────────────────────

    public override AstNode VisitParenExpr(AmberParser.ParenExprContext ctx)
        => Visit(ctx.expression());

    public override AstNode VisitPartExpr(AmberParser.PartExprContext ctx)
        => new PartAccessNode((ExpressionNode)Visit(ctx.expression()),
                              ctx.IDENTIFIER().GetText());

    public override AstNode VisitUnaryExpr(AmberParser.UnaryExprContext ctx)
    {
        var op = ctx.op.Type switch
        {
            AmberLexer.MINUS => UnaryOp.Negate,
            AmberLexer.TILDE => UnaryOp.BitwiseNot,
            AmberLexer.BANG  => UnaryOp.LogicalNot,
            _ => throw new InvalidOperationException($"Unknown unary op '{ctx.op.Text}'")
        };
        return new UnaryExprNode(op, (ExpressionNode)Visit(ctx.expression()));
    }

    public override AstNode VisitMulExpr(AmberParser.MulExprContext ctx)
    {
        var op = ctx.op.Type switch
        {
            AmberLexer.STAR    => BinaryOp.Mul,
            AmberLexer.SLASH   => BinaryOp.Div,
            AmberLexer.PERCENT => BinaryOp.Mod,
            _ => throw new InvalidOperationException($"Unknown op '{ctx.op.Text}'")
        };
        return new BinaryExprNode(op,
            (ExpressionNode)Visit(ctx.left), (ExpressionNode)Visit(ctx.right));
    }

    public override AstNode VisitAddExpr(AmberParser.AddExprContext ctx)
    {
        var op = ctx.op.Type switch
        {
            AmberLexer.PLUS  => BinaryOp.Add,
            AmberLexer.MINUS => BinaryOp.Sub,
            _ => throw new InvalidOperationException($"Unknown op '{ctx.op.Text}'")
        };
        return new BinaryExprNode(op,
            (ExpressionNode)Visit(ctx.left), (ExpressionNode)Visit(ctx.right));
    }

    public override AstNode VisitShiftExpr(AmberParser.ShiftExprContext ctx)
    {
        var op = ctx.op.Type switch
        {
            AmberLexer.LSHIFT => BinaryOp.Shl,
            AmberLexer.RSHIFT => BinaryOp.Shr,
            _ => throw new InvalidOperationException($"Unknown op '{ctx.op.Text}'")
        };
        return new BinaryExprNode(op,
            (ExpressionNode)Visit(ctx.left), (ExpressionNode)Visit(ctx.right));
    }

    public override AstNode VisitBandExpr(AmberParser.BandExprContext ctx)
        => new BinaryExprNode(BinaryOp.And,
            (ExpressionNode)Visit(ctx.left), (ExpressionNode)Visit(ctx.right));

    public override AstNode VisitBxorExpr(AmberParser.BxorExprContext ctx)
        => new BinaryExprNode(BinaryOp.Xor,
            (ExpressionNode)Visit(ctx.left), (ExpressionNode)Visit(ctx.right));

    public override AstNode VisitBorExpr(AmberParser.BorExprContext ctx)
        => new BinaryExprNode(BinaryOp.Or,
            (ExpressionNode)Visit(ctx.left), (ExpressionNode)Visit(ctx.right));

    public override AstNode VisitIdentExpr(AmberParser.IdentExprContext ctx)
        => new IdentifierNode(ctx.IDENTIFIER().GetText());

    public override AstNode VisitIntLitExpr(AmberParser.IntLitExprContext ctx)
        => new IntLiteralNode(long.Parse(ctx.INTEGER_LITERAL().GetText()));

    public override AstNode VisitHexLitExpr(AmberParser.HexLitExprContext ctx)
        => new IntLiteralNode(Convert.ToInt64(ctx.HEX_LITERAL().GetText()[2..], 16));

    public override AstNode VisitBoolLitExpr(AmberParser.BoolLitExprContext ctx)
        => new BoolLiteralNode(ctx.BOOL_LITERAL().GetText() == "true");

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AmberType ParseType(AmberParser.TypeSpecContext ctx)
    {
        if (ctx.BYTE() != null) return AmberType.Byte;
        if (ctx.WORD() != null) return AmberType.Word;
        if (ctx.LONG() != null) return AmberType.Long;
        if (ctx.BOOL() != null) return AmberType.Bool;
        throw new InvalidOperationException($"Unknown typeSpec '{ctx.GetText()}'");
    }

    private static AmberType ParseBaseType(AmberParser.BaseTypeContext ctx)
    {
        if (ctx.BYTE() != null) return AmberType.Byte;
        if (ctx.WORD() != null) return AmberType.Word;
        if (ctx.LONG() != null) return AmberType.Long;
        throw new InvalidOperationException($"Unknown baseType '{ctx.GetText()}'");
    }
}
