namespace AtomCCompiler
{
    /// <summary>
    /// Explicit states used by the lexer finite state machine.
    /// Keeping the states in an enum makes the DFA visible and easy to discuss in class.
    /// </summary>
    public enum LexerState
    {
        Start,
        Identifier,
        Zero,
        DecimalInteger,
        OctalInteger,
        HexPrefix,
        HexNumber,
        RealFraction,
        RealExponentStart,
        RealExponentSign,
        RealExponentDigits,
        CharLiteral,
        CharEscape,
        CharEnd,
        StringLiteral,
        StringEscape,
        Slash,
        LineComment,
        BlockComment,
        BlockCommentStar,
        Assign,
        Not,
        Less,
        Greater,
        And,
        Or,
        End,
        Error
    }
}
