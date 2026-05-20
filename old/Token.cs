namespace AtomCCompiler
{
    /// <summary>
    /// Represents a token emitted by the lexer.
    /// Each token stores its type, original lexeme, source location, and optional processed value.
    /// </summary>
    public sealed class Token
    {
        /// <summary>
        /// Creates a token object immediately after the lexer recognizes a token.
        /// </summary>
        /// <param name="type">Kind of token that was recognized.</param>
        /// <param name="lexeme">Original source text for the token.</param>
        /// <param name="line">1-based source line where the token begins.</param>
        /// <param name="column">1-based source column where the token begins.</param>
        /// <param name="value">Optional parsed value for literals and identifiers.</param>
        public Token(TokenType type, string lexeme, int line, int column, object? value = null)
        {
            Type = type;
            Lexeme = lexeme;
            Line = line;
            Column = column;
            Value = value;
        }

        /// <summary>
        /// Gets the token type.
        /// </summary>
        public TokenType Type { get; }

        /// <summary>
        /// Gets the exact source text used to create the token.
        /// </summary>
        public string Lexeme { get; }

        /// <summary>
        /// Gets the line where the token starts.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Gets the column where the token starts.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Gets the parsed value for literals.
        /// For example, 014 stores the numeric value 12 while keeping the original lexeme.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Returns the token start as a reusable SourcePosition object.
        /// </summary>
        public SourcePosition Position => new SourcePosition(Line, Column);
    }
}
