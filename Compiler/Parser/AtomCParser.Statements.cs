namespace AtomCcompiler.Compiler.Parser;

/// <summary>
/// This partial class file contains statement grammar rules.
/// These methods parse control flow, returns, expression statements, and compound blocks.
/// </summary>
public sealed partial class AtomCParser
{
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
}
