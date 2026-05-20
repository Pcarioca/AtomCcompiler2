using AtomCcompiler.Compiler.Diagnostics;

namespace AtomCcompiler.Compiler.Parser;

/// <summary>
/// This partial class file contains parser utility methods.
/// These helpers keep token inspection, token consumption, backtracking, and error reporting consistent.
/// </summary>
public sealed partial class AtomCParser
{
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
