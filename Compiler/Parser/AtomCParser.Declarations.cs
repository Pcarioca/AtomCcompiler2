namespace AtomCcompiler.Compiler.Parser;

/// <summary>
/// This partial class file contains declaration-related grammar rules.
/// These methods parse structures, variables, types, arrays, functions, and parameters.
/// </summary>
public sealed partial class AtomCParser
{
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
}
