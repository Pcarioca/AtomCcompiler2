namespace AtomCcompiler.Compiler;

/// <summary>
/// This class stores a token produced by the lexer.
/// </summary>
public sealed class Token
{
    /// <summary>
    /// This constructor initializes all token fields.
    /// </summary>
    public Token(TokenType type, string value, int line, int column)
    {
        // This line stores the token type.
        Type = type;

        // This line stores the exact source text for the token.
        Value = value;

        // This line stores the 1-based source line where the token starts.
        Line = line;

        // This line stores the 1-based source column where the token starts.
        Column = column;
    }

    /// <summary>
    /// This property exposes the token type.
    /// </summary>
    public TokenType Type { get; }

    /// <summary>
    /// This property exposes the original token text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// This property exposes the token line.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// This property exposes the token column.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// This method formats a readable token representation.
    /// </summary>
    public override string ToString()
    {
        // This return statement builds a simple debug-friendly text format.
        return $"{Type}('{Value}') at {Line}:{Column}";
    }
}
