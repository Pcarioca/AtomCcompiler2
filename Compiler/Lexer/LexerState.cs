namespace AtomCcompiler.Compiler.Lexer;

/// <summary>
/// Explicit states used by the lexer finite state machine.
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
    BinaryPrefix,
    BinaryNumber,
    Base64Prefix,
    Base64Number,
    Base64Padding1,
    Base64Padding2,

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
