using AtomCcompiler.Compiler.Diagnostics;

namespace AtomCcompiler.Compiler.Parser;

/// <summary>
/// This class implements a simple recursive-descent parser for Atom C.
/// The parser validates syntax only and does not build an abstract syntax tree.
/// </summary>
public sealed class AtomCParser
{
    /// <summary>
    /// These keywords are accepted as base types in the current project.
    /// The official grammar uses a smaller set, but the existing repo examples also use float, string, and bool.
    /// </summary>
    private static readonly HashSet<string> TypeKeywords =
    [
        "int",
        "double",
        "char",
        "float",
        "string",
        "bool"
    ];

    /// <summary>
    /// This field stores the token stream produced by the lexer.
    /// </summary>
    private readonly IReadOnlyList<Token> _tokens;

    /// <summary>
    /// This field stores the syntax diagnostics produced during parsing.
    /// </summary>
    private readonly List<SyntaxError> _errors = [];

    /// <summary>
    /// This field stores the current token index.
    /// </summary>
    private int _index;

    /// <summary>
    /// This constructor stores the token stream that must be parsed.
    /// </summary>
    public AtomCParser(IReadOnlyList<Token> tokens)
    {
        // This line stores the full token list for all parser rules.
        _tokens = tokens;
    }

    /// <summary>
    /// This property exposes the current token.
    /// </summary>
    private Token Current => Peek(0);

    /// <summary>
    /// This property exposes the previously consumed token.
    /// </summary>
    private Token Previous => Peek(-1);

    /// <summary>
    /// This method runs the full syntax analysis starting from the unit rule.
    /// </summary>
    public ParserResult Parse()
    {
        // This line attempts to parse the whole compilation unit.
        Unit();

        // This branch requires the logical end-of-file token when no prior syntax error exists.
        if (!HasErrors && !Expect(TokenType.EndOfFile, "expected end of file"))
        {
            // This block intentionally has no extra logic because Expect already reported the error.
        }

        // This return exposes all parser diagnostics to the compiler pipeline.
        return new ParserResult(_errors);
    }

    /// <summary>
    /// This method implements the unit grammar rule.
    /// A unit is a sequence of structure definitions, function definitions, or variable definitions.
    /// </summary>
    private bool Unit()
    {
        // This loop continues until the parser reaches the logical end of input or a fatal syntax error.
        while (!IsAtEnd() && !HasErrors)
        {
            if (StructDef())
            {
                continue;
            }

            if (FnDef())
            {
                continue;
            }

            if (VarDef())
            {
                continue;
            }

            AddError("expected structure definition, function definition, or variable definition");
            return false;
        }

        return !HasErrors;
    }

    /// <summary>
    /// This method implements the structDef grammar rule.
    /// It only starts when the upcoming tokens look exactly like "struct ID {".
    /// </summary>
    private bool StructDef()
    {
        var start = Save();

        // This branch avoids confusing "struct ID variable;" with a structure definition.
        if (!IsStructDefinitionAhead())
        {
            Restore(start);
            return false;
        }

        ConsumeKeyword("struct");

        // This branch reports a committed error because a structure definition has definitely started.
        if (!Expect(TokenType.Identifier, "expected identifier after 'struct'"))
        {
            return false;
        }

        if (!Expect(TokenType.LeftBrace, "expected '{' after structure name"))
        {
            return false;
        }

        // This loop accepts zero or more field declarations inside the structure body.
        while (!HasErrors && !Check(TokenType.RightBrace))
        {
            if (VarDef())
            {
                continue;
            }

            AddError("expected variable definition inside structure");
            return false;
        }

        if (!Expect(TokenType.RightBrace, "expected '}' after structure fields"))
        {
            return false;
        }

        if (!Expect(TokenType.Semicolon, "expected ';' after structure definition"))
        {
            return false;
        }

        return !HasErrors;
    }

    /// <summary>
    /// This method implements the varDef grammar rule for top-level and structure declarations.
    /// Repo-local initializer extensions are kept for local variables only, not for top-level declarations.
    /// </summary>
    private bool VarDef()
    {
        return VarDef(allowInitializer: false);
    }

    /// <summary>
    /// This helper implements local variable definitions inside compound statements.
    /// The current repo examples use initialized local declarations such as "int x = 10;".
    /// </summary>
    private bool LocalVarDef()
    {
        return VarDef(allowInitializer: true);
    }

    /// <summary>
    /// This method implements the shared variable-definition logic.
    /// The allowInitializer flag keeps repo-specific local declaration support isolated and easy to explain.
    /// </summary>
    private bool VarDef(bool allowInitializer)
    {
        var start = Save();

        // This branch returns quietly when the input does not begin with a valid base type.
        if (!TypeBase())
        {
            Restore(start);
            return false;
        }

        // This branch reports a committed error because the declaration has already started with a type.
        if (!Expect(TokenType.Identifier, "expected identifier after type"))
        {
            return false;
        }

        if (ArrayDecl())
        {
            // This block intentionally has no extra logic because ArrayDecl already consumed the brackets.
        }

        if (allowInitializer && Consume(TokenType.Assign))
        {
            // This branch requires an initializer expression once '=' is present.
            if (!Expr())
            {
                AddError("expected expression after '='");
                return false;
            }
        }

        // This loop implements the repo-compatible extension for comma-separated declarators.
        while (Consume(TokenType.Comma))
        {
            if (!Expect(TokenType.Identifier, "expected identifier after ',' in variable definition"))
            {
                return false;
            }

            if (ArrayDecl())
            {
                // This block intentionally has no extra logic because ArrayDecl already consumed the brackets.
            }

            if (allowInitializer && Consume(TokenType.Assign))
            {
                if (!Expr())
                {
                    AddError("expected expression after '='");
                    return false;
                }
            }
        }

        if (!Expect(TokenType.Semicolon, "expected ';' after variable definition"))
        {
            return false;
        }

        return !HasErrors;
    }

    /// <summary>
    /// This method implements the typeBase grammar rule.
    /// It accepts the official Atom C types plus the current repo's existing type keywords.
    /// </summary>
    private bool TypeBase()
    {
        var start = Save();

        if (Check(TokenType.Keyword) && TypeKeywords.Contains(Current.Value))
        {
            Advance();
            return true;
        }

        if (ConsumeKeyword("struct"))
        {
            // This branch requires the structure name after the struct keyword.
            if (!Expect(TokenType.Identifier, "expected identifier after 'struct'"))
            {
                return false;
            }

            return true;
        }

        Restore(start);
        return false;
    }

    /// <summary>
    /// This method implements the arrayDecl grammar rule.
    /// The current repo examples use expressions inside array declarations, so this is broader than the official CT_INT-only form.
    /// </summary>
    private bool ArrayDecl()
    {
        var start = Save();

        if (!Consume(TokenType.LeftBracket))
        {
            Restore(start);
            return false;
        }

        // This branch keeps the empty array form "[]" valid.
        if (!Check(TokenType.RightBracket))
        {
            if (!Expr())
            {
                AddError("expected expression inside array declaration");
                return false;
            }
        }

        if (!Expect(TokenType.RightBracket, "expected ']' after array declaration"))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// This method implements the fnDef grammar rule.
    /// A function definition is distinguished from a variable definition after the return type and function name.
    /// </summary>
    private bool FnDef()
    {
        var start = Save();

        // This branch checks the function return type, which may be a typeBase or the void keyword.
        if (!TypeBase())
        {
            Restore(start);

            if (!ConsumeKeyword("void"))
            {
                return false;
            }
        }

        // This branch returns quietly for alternatives until the parser sees the opening parenthesis.
        if (!Consume(TokenType.Identifier))
        {
            Restore(start);
            return false;
        }

        if (!Consume(TokenType.LeftParen))
        {
            Restore(start);
            return false;
        }

        // This branch parses the optional parameter list.
        if (!Check(TokenType.RightParen) && CanStartTypeBase())
        {
            if (!FnParam())
            {
                AddError("expected function parameter");
                return false;
            }

            while (Consume(TokenType.Comma))
            {
                if (!FnParam())
                {
                    AddError("expected function parameter after ','");
                    return false;
                }
            }
        }

        if (!Expect(TokenType.RightParen, "expected ')' after function parameters"))
        {
            return false;
        }

        if (!StmCompound())
        {
            AddError("expected compound statement after function header");
            return false;
        }

        return !HasErrors;
    }

    /// <summary>
    /// This method implements the fnParam grammar rule.
    /// Function parameters use the same base type syntax as declarations and may include an optional array suffix.
    /// </summary>
    private bool FnParam()
    {
        var start = Save();

        if (!TypeBase())
        {
            Restore(start);
            return false;
        }

        if (!Expect(TokenType.Identifier, "expected parameter name"))
        {
            return false;
        }

        if (ArrayDecl())
        {
            // This block intentionally has no extra logic because ArrayDecl already consumed the brackets.
        }

        return !HasErrors;
    }

    /// <summary>
    /// This method implements the stm grammar rule.
    /// It dispatches to the correct statement form based on the current token.
    /// </summary>
    private bool Stm()
    {
        var start = Save();

        if (StmCompound())
        {
            return true;
        }

        Restore(start);

        if (ConsumeKeyword("if"))
        {
            if (!Expect(TokenType.LeftParen, "expected '(' after if"))
            {
                return false;
            }

            if (!Expr())
            {
                AddError("expected expression after '(' in if condition");
                return false;
            }

            if (!Expect(TokenType.RightParen, "expected ')' after if condition"))
            {
                return false;
            }

            if (!Stm())
            {
                AddError("expected statement after if condition");
                return false;
            }

            if (ConsumeKeyword("else") && !Stm())
            {
                AddError("expected statement after else");
                return false;
            }

            return !HasErrors;
        }

        if (ConsumeKeyword("while"))
        {
            if (!Expect(TokenType.LeftParen, "expected '(' after while"))
            {
                return false;
            }

            if (!Expr())
            {
                AddError("expected expression after '(' in while condition");
                return false;
            }

            if (!Expect(TokenType.RightParen, "expected ')' after while condition"))
            {
                return false;
            }

            if (!Stm())
            {
                AddError("expected statement after while condition");
                return false;
            }

            return !HasErrors;
        }

        if (ConsumeKeyword("for"))
        {
            if (!Expect(TokenType.LeftParen, "expected '(' after for"))
            {
                return false;
            }

            if (!Check(TokenType.Semicolon) && CanStartExpr())
            {
                if (!Expr())
                {
                    AddError("expected expression in for initializer");
                    return false;
                }
            }

            if (!Expect(TokenType.Semicolon, "expected ';' after for initializer"))
            {
                return false;
            }

            if (!Check(TokenType.Semicolon) && CanStartExpr())
            {
                if (!Expr())
                {
                    AddError("expected expression in for condition");
                    return false;
                }
            }

            if (!Expect(TokenType.Semicolon, "expected ';' after for condition"))
            {
                return false;
            }

            if (!Check(TokenType.RightParen) && CanStartExpr())
            {
                if (!Expr())
                {
                    AddError("expected expression in for increment");
                    return false;
                }
            }

            if (!Expect(TokenType.RightParen, "expected ')' after for statement"))
            {
                return false;
            }

            if (!Stm())
            {
                AddError("expected statement after for header");
                return false;
            }

            return !HasErrors;
        }

        if (ConsumeKeyword("break"))
        {
            return Expect(TokenType.Semicolon, "expected ';' after break statement");
        }

        if (ConsumeKeyword("continue"))
        {
            return Expect(TokenType.Semicolon, "expected ';' after continue statement");
        }

        if (ConsumeKeyword("return"))
        {
            if (CanStartExpr())
            {
                if (!Expr())
                {
                    AddError("expected expression after return");
                    return false;
                }
            }

            return Expect(TokenType.Semicolon, "expected ';' after return statement");
        }

        // This branch handles the empty statement and normal expression statement.
        if (Consume(TokenType.Semicolon))
        {
            return true;
        }

        if (CanStartExpr())
        {
            if (!Expr())
            {
                return false;
            }

            return Expect(TokenType.Semicolon, "expected ';' after expression");
        }

        return false;
    }

    /// <summary>
    /// This method implements the stmCompound grammar rule.
    /// A compound statement contains a mixed list of local declarations and statements.
    /// </summary>
    private bool StmCompound()
    {
        var start = Save();

        if (!Consume(TokenType.LeftBrace))
        {
            Restore(start);
            return false;
        }

        // This loop repeatedly tries a local declaration first and then a statement.
        while (!HasErrors && !Check(TokenType.RightBrace))
        {
            // This branch lets the final Expect on '}' produce the specific missing-brace diagnostic at end of file.
            if (IsAtEnd())
            {
                break;
            }

            var itemStart = Save();

            if (LocalVarDef())
            {
                continue;
            }

            Restore(itemStart);

            if (Stm())
            {
                continue;
            }

            Restore(itemStart);
            AddError("expected variable definition or statement inside compound statement");
            return false;
        }

        if (!Expect(TokenType.RightBrace, "expected '}' after compound statement"))
        {
            return false;
        }

        return !HasErrors;
    }

    /// <summary>
    /// This method implements the expr grammar rule.
    /// Expressions start with assignment because assignment has the lowest precedence.
    /// </summary>
    private bool Expr()
    {
        return ExprAssign();
    }

    /// <summary>
    /// This method implements assignment expressions.
    /// Assignment is parsed separately because it is right-associative.
    /// </summary>
    private bool ExprAssign()
    {
        var start = Save();

        if (ExprUnary())
        {
            if (Consume(TokenType.Assign))
            {
                if (!ExprAssign())
                {
                    AddError("expected expression after '='");
                    return false;
                }

                return true;
            }
        }

        Restore(start);
        return ExprOr();
    }

    /// <summary>
    /// This method implements logical OR expressions.
    /// The loop replaces the left-recursive grammar exprOr -> exprOr OR exprAnd.
    /// </summary>
    private bool ExprOr()
    {
        if (!ExprAnd())
        {
            return false;
        }

        while (Consume(TokenType.Or))
        {
            if (!ExprAnd())
            {
                AddError("expected expression after '||'");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// This method implements logical AND expressions.
    /// The loop replaces the left-recursive grammar exprAnd -> exprAnd AND exprEq.
    /// </summary>
    private bool ExprAnd()
    {
        if (!ExprEq())
        {
            return false;
        }

        while (Consume(TokenType.And))
        {
            if (!ExprEq())
            {
                AddError("expected expression after '&&'");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// This method implements equality expressions.
    /// The loop replaces the left-recursive grammar exprEq -> exprEq (==|!=) exprRel.
    /// </summary>
    private bool ExprEq()
    {
        if (!ExprRel())
        {
            return false;
        }

        while (Consume(TokenType.Equal) || Consume(TokenType.NotEqual))
        {
            if (!ExprRel())
            {
                AddError("expected expression after equality operator");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// This method implements relational expressions.
    /// The loop replaces the left-recursive grammar exprRel -> exprRel relop exprAdd.
    /// </summary>
    private bool ExprRel()
    {
        if (!ExprAdd())
        {
            return false;
        }

        while (Consume(TokenType.Less) || Consume(TokenType.LessOrEqual) || Consume(TokenType.Greater) || Consume(TokenType.GreaterOrEqual))
        {
            if (!ExprAdd())
            {
                AddError("expected expression after relational operator");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// This method implements additive expressions.
    /// The loop replaces the left-recursive grammar exprAdd -> exprAdd (+|-) exprMul.
    /// </summary>
    private bool ExprAdd()
    {
        if (!ExprMul())
        {
            return false;
        }

        while (Consume(TokenType.Plus) || Consume(TokenType.Minus))
        {
            if (!ExprMul())
            {
                AddError("expected expression after additive operator");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// This method implements multiplicative expressions.
    /// The loop replaces the left-recursive grammar exprMul -> exprMul (*|/|%) exprCast.
    /// </summary>
    private bool ExprMul()
    {
        if (!ExprCast())
        {
            return false;
        }

        while (Consume(TokenType.Star) || Consume(TokenType.Slash) || Consume(TokenType.Percent))
        {
            if (!ExprCast())
            {
                AddError("expected expression after multiplicative operator");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// This method implements cast expressions.
    /// We try the cast form first and restore the parser position if the tokens actually form a normal parenthesized expression.
    /// </summary>
    private bool ExprCast()
    {
        var start = Save();

        if (Consume(TokenType.LeftParen))
        {
            var afterLeftParen = Save();

            if (TypeBase())
            {
                if (ArrayDecl())
                {
                    // This block intentionally has no extra logic because ArrayDecl already consumed the brackets.
                }

                if (Consume(TokenType.RightParen))
                {
                    if (!ExprCast())
                    {
                        AddError("expected expression after cast");
                        return false;
                    }

                    return true;
                }
            }

            // This restore lets the parser reinterpret the same opening parenthesis as a normal primary expression.
            Restore(start);
        }

        Restore(start);
        return ExprUnary();
    }

    /// <summary>
    /// This method implements unary expressions.
    /// Unary operators recurse on the same precedence level because they chain right-to-left.
    /// </summary>
    private bool ExprUnary()
    {
        if (Consume(TokenType.Minus) || Consume(TokenType.Not))
        {
            if (!ExprUnary())
            {
                AddError("expected expression after unary operator");
                return false;
            }

            return true;
        }

        return ExprPostfix();
    }

    /// <summary>
    /// This method implements postfix expressions.
    /// The loop replaces the left-recursive grammar exprPostfix -> exprPostfix postfixPart.
    /// </summary>
    private bool ExprPostfix()
    {
        if (!ExprPrimary())
        {
            return false;
        }

        while (true)
        {
            if (Consume(TokenType.LeftBracket))
            {
                if (!Expr())
                {
                    AddError("expected expression inside array index");
                    return false;
                }

                if (!Expect(TokenType.RightBracket, "expected ']' after array index"))
                {
                    return false;
                }

                continue;
            }

            if (Consume(TokenType.Dot))
            {
                if (!Expect(TokenType.Identifier, "expected field name after '.'"))
                {
                    return false;
                }

                continue;
            }

            break;
        }

        return true;
    }

    /// <summary>
    /// This method implements primary expressions.
    /// Primary expressions include identifiers, function calls, literals, and parenthesized expressions.
    /// </summary>
    private bool ExprPrimary()
    {
        if (Consume(TokenType.Identifier))
        {
            // This branch parses an optional function call after an identifier.
            if (Consume(TokenType.LeftParen))
            {
                if (!Check(TokenType.RightParen))
                {
                    if (!Expr())
                    {
                        AddError("expected expression after '(' in function call");
                        return false;
                    }

                    while (Consume(TokenType.Comma))
                    {
                        if (!Expr())
                        {
                            AddError("expected expression after ',' in function call");
                            return false;
                        }
                    }
                }

                if (!Expect(TokenType.RightParen, "expected ')' after function call"))
                {
                    return false;
                }
            }

            return true;
        }

        if (CheckLiteral())
        {
            Advance();
            return true;
        }

        if (CheckKeyword("true") || CheckKeyword("false"))
        {
            Advance();
            return true;
        }

        if (Consume(TokenType.LeftParen))
        {
            if (!Expr())
            {
                AddError("expected expression after '('");
                return false;
            }

            if (!Expect(TokenType.RightParen, "expected ')' after expression"))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// This method reports whether the parser is currently positioned at the end-of-file token.
    /// </summary>
    private bool IsAtEnd()
    {
        return Check(TokenType.EndOfFile);
    }

    /// <summary>
    /// This method checks whether the current token matches a specific token type.
    /// </summary>
    private bool Check(TokenType type)
    {
        return Current.Type == type;
    }

    /// <summary>
    /// This method checks whether the current token is a specific keyword.
    /// The parser uses keyword text because the lexer groups all keywords under one token kind.
    /// </summary>
    private bool CheckKeyword(string keyword)
    {
        return Check(TokenType.Keyword) && string.Equals(Current.Value, keyword, StringComparison.Ordinal);
    }

    /// <summary>
    /// This method checks whether the current token is an identifier.
    /// </summary>
    private bool CheckIdentifier()
    {
        return Check(TokenType.Identifier);
    }

    /// <summary>
    /// This method checks whether the current token is one of the integer-like literal forms used by the current lexer.
    /// </summary>
    private bool CheckIntegerLikeLiteral()
    {
        return Check(TokenType.IntegerLiteral)
               || Check(TokenType.HexLiteral)
               || Check(TokenType.OctalLiteral)
               || Check(TokenType.BinaryLiteral)
               || Check(TokenType.Base64Literal);
    }

    /// <summary>
    /// This method checks whether the current token is any literal token.
    /// </summary>
    private bool CheckLiteral()
    {
        return CheckIntegerLikeLiteral()
               || Check(TokenType.RealLiteral)
               || Check(TokenType.CharLiteral)
               || Check(TokenType.StringLiteral);
    }

    /// <summary>
    /// This method reports whether the current token can start an expression.
    /// </summary>
    private bool CanStartExpr()
    {
        return CheckIdentifier()
               || CheckLiteral()
               || Check(TokenType.LeftParen)
               || Check(TokenType.Minus)
               || Check(TokenType.Not)
               || CheckKeyword("true")
               || CheckKeyword("false");
    }

    /// <summary>
    /// This method reports whether the current token can start a typeBase.
    /// </summary>
    private bool CanStartTypeBase()
    {
        return CheckKeyword("struct")
               || (Check(TokenType.Keyword) && TypeKeywords.Contains(Current.Value));
    }

    /// <summary>
    /// This method consumes the current token when it matches the requested type.
    /// </summary>
    private bool Consume(TokenType type)
    {
        if (!Check(type))
        {
            return false;
        }

        Advance();
        return true;
    }

    /// <summary>
    /// This method consumes the current token when it matches the requested keyword.
    /// </summary>
    private bool ConsumeKeyword(string keyword)
    {
        if (!CheckKeyword(keyword))
        {
            return false;
        }

        Advance();
        return true;
    }

    /// <summary>
    /// This method requires a terminal token and reports a SyntaxError when it is missing.
    /// </summary>
    private bool Expect(TokenType type, string message)
    {
        if (Consume(type))
        {
            return true;
        }

        AddError(message);
        return false;
    }

    /// <summary>
    /// This method requires a keyword and reports a SyntaxError when it is missing.
    /// </summary>
    private bool ExpectKeyword(string keyword, string message)
    {
        if (ConsumeKeyword(keyword))
        {
            return true;
        }

        AddError(message);
        return false;
    }

    /// <summary>
    /// This method consumes the current token and moves the parser forward.
    /// </summary>
    private Token Advance()
    {
        var token = Current;

        if (_index < _tokens.Count - 1)
        {
            _index++;
        }

        return token;
    }

    /// <summary>
    /// This method saves the current parser index.
    /// A plain integer is enough for this parser and keeps the implementation easy to follow.
    /// </summary>
    private int Save()
    {
        return _index;
    }

    /// <summary>
    /// This method restores the parser to a previously saved index.
    /// It is used when trying alternatives without committing to one too early.
    /// </summary>
    private void Restore(int position)
    {
        _index = position;
    }

    /// <summary>
    /// This method returns a token at a relative offset from the current parser position.
    /// </summary>
    private Token Peek(int offset)
    {
        var targetIndex = _index + offset;

        if (targetIndex < 0)
        {
            return _tokens[0];
        }

        if (targetIndex >= _tokens.Count)
        {
            return _tokens[^1];
        }

        return _tokens[targetIndex];
    }

    /// <summary>
    /// This method reports whether the parser already produced a syntax error.
    /// The parser keeps error recovery intentionally minimal, so one committed syntax error is enough to stop.
    /// </summary>
    private bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// This method adds one syntax diagnostic at the current token position.
    /// Additional parser errors are suppressed so the first committed syntax error stays clear.
    /// </summary>
    private void AddError(string message)
    {
        if (HasErrors)
        {
            return;
        }

        _errors.Add(new SyntaxError(message, Current.Line, Current.Column));
    }

    /// <summary>
    /// This method checks whether the upcoming tokens begin a structure definition rather than a struct-based variable declaration.
    /// </summary>
    private bool IsStructDefinitionAhead()
    {
        return CheckKeyword("struct")
               && Peek(1).Type == TokenType.Identifier
               && Peek(2).Type == TokenType.LeftBrace;
    }
}
