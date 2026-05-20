namespace AtomCcompiler.Compiler.Parser;

/// <summary>
/// This partial class file contains expression grammar rules.
/// The methods are ordered from lowest precedence to highest precedence.
/// Left-recursive grammar rules are implemented as loops, while assignment stays right-associative.
/// </summary>
public sealed partial class AtomCParser
{
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
}
