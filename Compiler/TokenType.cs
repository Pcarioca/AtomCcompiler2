namespace AtomCcompiler.Compiler;

/// <summary>
/// This enum lists all token kinds recognized by the Atom C lexer.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// This token is used for user-defined identifiers such as variable names.
    /// </summary>
    Identifier,

    /// <summary>
    /// This token is used for reserved keywords such as if, else, and while.
    /// </summary>
    Keyword,

    /// <summary>
    /// This token represents an integer numeric literal.
    /// </summary>
    IntegerLiteral,

    /// <summary>
    /// This token represents a floating-point numeric literal.
    /// </summary>
    RealLiteral,

    /// <summary>
    /// This token represents a hexadecimal numeric literal.
    /// </summary>
    HexLiteral,

    /// <summary>
    /// This token represents an octal numeric literal.
    /// </summary>
    OctalLiteral,

    /// <summary>
    /// This token represents a binary numeric literal.
    /// </summary>
    BinaryLiteral,

    /// <summary>
    /// This token represents a base-64 numeric literal.
    /// </summary>
    Base64Literal,

    /// <summary>
    /// This token represents a character literal.
    /// </summary>
    CharLiteral,

    /// <summary>
    /// This token represents a string literal.
    /// </summary>
    StringLiteral,

    /// <summary>
    /// This token represents the '=' operator.
    /// </summary>
    Assign,

    /// <summary>
    /// This token represents the '==' operator.
    /// </summary>
    Equal,

    /// <summary>
    /// This token represents the '!' operator.
    /// </summary>
    Not,

    /// <summary>
    /// This token represents the '!=' operator.
    /// </summary>
    NotEqual,

    /// <summary>
    /// This token represents the '<' operator.
    /// </summary>
    Less,

    /// <summary>
    /// This token represents the '<=' operator.
    /// </summary>
    LessOrEqual,

    /// <summary>
    /// This token represents the '>' operator.
    /// </summary>
    Greater,

    /// <summary>
    /// This token represents the '>=' operator.
    /// </summary>
    GreaterOrEqual,

    /// <summary>
    /// This token represents the '&&' operator.
    /// </summary>
    And,

    /// <summary>
    /// This token represents the '||' operator.
    /// </summary>
    Or,

    /// <summary>
    /// This token represents the '+' operator.
    /// </summary>
    Plus,

    /// <summary>
    /// This token represents the '-' operator.
    /// </summary>
    Minus,

    /// <summary>
    /// This token represents the '*' operator.
    /// </summary>
    Star,

    /// <summary>
    /// This token represents the '/' operator.
    /// </summary>
    Slash,

    /// <summary>
    /// This token represents the '%' operator.
    /// </summary>
    Percent,

    /// <summary>
    /// This token represents the ';' symbol.
    /// </summary>
    Semicolon,

    /// <summary>
    /// This token represents the ',' symbol.
    /// </summary>
    Comma,

    /// <summary>
    /// This token represents the '.' symbol.
    /// </summary>
    Dot,

    /// <summary>
    /// This token represents the '(' symbol.
    /// </summary>
    LeftParen,

    /// <summary>
    /// This token represents the ')' symbol.
    /// </summary>
    RightParen,

    /// <summary>
    /// This token represents the '{' symbol.
    /// </summary>
    LeftBrace,

    /// <summary>
    /// This token represents the '}' symbol.
    /// </summary>
    RightBrace,

    /// <summary>
    /// This token represents the '[' symbol.
    /// </summary>
    LeftBracket,

    /// <summary>
    /// This token represents the ']' symbol.
    /// </summary>
    RightBracket,

    /// <summary>
    /// This token marks the logical end of the source input.
    /// </summary>
    EndOfFile
}
