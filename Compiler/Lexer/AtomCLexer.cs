using System.Text;
using AtomCcompiler.Compiler.Diagnostics;

namespace AtomCcompiler.Compiler.Lexer;

/// <summary>
/// Beginner-friendly character-by-character lexer for Atom C.
/// </summary>
public sealed class AtomCLexer
{
    private static readonly HashSet<string> Keywords =
    [
        "if", "else", "while", "for", "return", "break", "continue",
        "int", "float", "double", "char", "string", "void", "bool",
        "true", "false", "struct"
    ];

    private string _source = string.Empty;
    private int _index;
    private int _line;
    private int _column;

    /// <summary>
    /// Tokenizes source code that is already split into lines.
    /// </summary>
    public LexerResult Tokenize(IReadOnlyList<string> lines)
    {
        _source = string.Join('\n', lines);
        _index = 0;
        _line = 1;
        _column = 1;

        var tokens = new List<Token>();
        var errors = new List<LexicalError>();

        while (!IsAtEnd())
        {
            var token = ReadNextToken(errors);
            if (token is not null)
            {
                tokens.Add(token);
            }
        }

        tokens.Add(new Token(TokenType.EndOfFile, "<EOF>", lines.Count + 1, 1));
        return new LexerResult(tokens, errors);
    }

    private Token? ReadNextToken(ICollection<LexicalError> errors)
    {
        var state = LexerState.Start;
        var lexeme = new StringBuilder();
        var tokenStart = CurrentPosition();
        var hasFractionDigits = false;

        while (true)
        {
            switch (state)
            {
                case LexerState.Start:
                    SkipWhitespace();
                    tokenStart = CurrentPosition();
                    lexeme.Clear();
                    hasFractionDigits = false;

                    if (IsAtEnd())
                    {
                        return null;
                    }

                    var current = Peek();

                    if (IsIdentifierStart(current))
                    {
                        lexeme.Append(Advance());
                        state = LexerState.Identifier;
                        continue;
                    }

                    if (IsDigit(current))
                    {
                        var digit = Advance();
                        lexeme.Append(digit);
                        state = digit == '0' ? LexerState.Zero : LexerState.DecimalInteger;
                        continue;
                    }

                    switch (current)
                    {
                        case '\'':
                            lexeme.Append(Advance());
                            state = LexerState.CharLiteral;
                            continue;

                        case '"':
                            lexeme.Append(Advance());
                            state = LexerState.StringLiteral;
                            continue;

                        case '/':
                            lexeme.Append(Advance());
                            state = LexerState.Slash;
                            continue;

                        case '=':
                            lexeme.Append(Advance());
                            state = LexerState.Assign;
                            continue;

                        case '!':
                            lexeme.Append(Advance());
                            state = LexerState.Not;
                            continue;

                        case '<':
                            lexeme.Append(Advance());
                            state = LexerState.Less;
                            continue;

                        case '>':
                            lexeme.Append(Advance());
                            state = LexerState.Greater;
                            continue;

                        case '&':
                            lexeme.Append(Advance());
                            state = LexerState.And;
                            continue;

                        case '|':
                            lexeme.Append(Advance());
                            state = LexerState.Or;
                            continue;

                        case '+':
                            return CreateToken(TokenType.Plus, Advance().ToString(), tokenStart);

                        case '-':
                            return CreateToken(TokenType.Minus, Advance().ToString(), tokenStart);

                        case '*':
                            return CreateToken(TokenType.Star, Advance().ToString(), tokenStart);

                        case '%':
                            return CreateToken(TokenType.Percent, Advance().ToString(), tokenStart);

                        case ';':
                            return CreateToken(TokenType.Semicolon, Advance().ToString(), tokenStart);

                        case ',':
                            return CreateToken(TokenType.Comma, Advance().ToString(), tokenStart);

                        case '.':
                            return CreateToken(TokenType.Dot, Advance().ToString(), tokenStart);

                        case '(':
                            return CreateToken(TokenType.LeftParen, Advance().ToString(), tokenStart);

                        case ')':
                            return CreateToken(TokenType.RightParen, Advance().ToString(), tokenStart);

                        case '{':
                            return CreateToken(TokenType.LeftBrace, Advance().ToString(), tokenStart);

                        case '}':
                            return CreateToken(TokenType.RightBrace, Advance().ToString(), tokenStart);

                        case '[':
                            return CreateToken(TokenType.LeftBracket, Advance().ToString(), tokenStart);

                        case ']':
                            return CreateToken(TokenType.RightBracket, Advance().ToString(), tokenStart);

                        default:
                            AddError(errors, tokenStart, $"Unexpected character '{DisplayCharacter(current)}'.");
                            Advance();
                            return null;
                    }

                case LexerState.Identifier:
                    if (!IsAtEnd() && IsIdentifierPart(Peek()))
                    {
                        lexeme.Append(Advance());
                        continue;
                    }

                    var identifier = lexeme.ToString();
                    var identifierType = Keywords.Contains(identifier) ? TokenType.Keyword : TokenType.Identifier;
                    return CreateToken(identifierType, identifier, tokenStart);

                case LexerState.Zero:
                    if (IsAtEnd())
                    {
                        return CreateToken(TokenType.IntegerLiteral, lexeme.ToString(), tokenStart);
                    }

                    current = Peek();

                    if (current is 'x' or 'X')
                    {
                        lexeme.Append(Advance());
                        state = LexerState.HexPrefix;
                        continue;
                    }

                    if (current is 'b' or 'B')
                    {
                        lexeme.Append(Advance());
                        state = LexerState.BinaryPrefix;
                        continue;
                    }

                    if (current is 'z' or 'Z')
                    {
                        lexeme.Append(Advance());
                        state = LexerState.Base64Prefix;
                        continue;
                    }

                    if (current == '.')
                    {
                        lexeme.Append(Advance());
                        state = LexerState.RealFraction;
                        continue;
                    }

                    if (current is 'e' or 'E')
                    {
                        lexeme.Append(Advance());
                        state = LexerState.RealExponentStart;
                        continue;
                    }

                    if (IsOctalDigit(current))
                    {
                        lexeme.Append(Advance());
                        state = LexerState.OctalInteger;
                        continue;
                    }

                    if (current is '8' or '9')
                    {
                        lexeme.Append(Advance());
                        ConsumeNumberTail(lexeme);
                        AddError(errors, tokenStart, "Invalid octal literal.");
                        return null;
                    }

                    if (IsIdentifierStart(current))
                    {
                        lexeme.Append(Advance());
                        ConsumeNumberTail(lexeme);
                        AddError(errors, tokenStart, "Invalid number format.");
                        return null;
                    }

                    return CreateToken(TokenType.IntegerLiteral, lexeme.ToString(), tokenStart);

                case LexerState.DecimalInteger:
                    if (IsAtEnd())
                    {
                        return CreateToken(TokenType.IntegerLiteral, lexeme.ToString(), tokenStart);
                    }

                    current = Peek();

                    if (IsDigit(current))
                    {
                        lexeme.Append(Advance());
                        continue;
                    }

                    if (current == '.')
                    {
                        lexeme.Append(Advance());
                        state = LexerState.RealFraction;
                        continue;
                    }

                    if (current is 'e' or 'E')
                    {
                        lexeme.Append(Advance());
                        state = LexerState.RealExponentStart;
                        continue;
                    }

                    if (IsIdentifierStart(current))
                    {
                        lexeme.Append(Advance());
                        ConsumeNumberTail(lexeme);
                        AddError(errors, tokenStart, "Invalid number format.");
                        return null;
                    }

                    return CreateToken(TokenType.IntegerLiteral, lexeme.ToString(), tokenStart);

                case LexerState.OctalInteger:
                    if (IsAtEnd())
                    {
                        return CreateToken(TokenType.OctalLiteral, lexeme.ToString(), tokenStart);
                    }

                    current = Peek();

                    if (IsOctalDigit(current))
                    {
                        lexeme.Append(Advance());
                        continue;
                    }

                    if (current is '8' or '9')
                    {
                        lexeme.Append(Advance());
                        ConsumeNumberTail(lexeme);
                        AddError(errors, tokenStart, "Invalid octal literal.");
                        return null;
                    }

                    if (IsIdentifierStart(current))
                    {
                        lexeme.Append(Advance());
                        ConsumeNumberTail(lexeme);
                        AddError(errors, tokenStart, "Invalid number format.");
                        return null;
                    }

                    return CreateToken(TokenType.OctalLiteral, lexeme.ToString(), tokenStart);

                case LexerState.HexPrefix:
                    if (IsAtEnd() || !IsHexDigit(Peek()))
                    {
                        AddError(errors, tokenStart, "Invalid hexadecimal literal, expected at least one hex digit after 0x.");
                        return null;
                    }

                    lexeme.Append(Advance());
                    state = LexerState.HexNumber;
                    continue;

                case LexerState.HexNumber:
                    if (IsAtEnd())
                    {
                        return CreateToken(TokenType.HexLiteral, lexeme.ToString(), tokenStart);
                    }

                    current = Peek();

                    if (IsHexDigit(current))
                    {
                        lexeme.Append(Advance());
                        continue;
                    }

                    if (IsIdentifierStart(current))
                    {
                        lexeme.Append(Advance());
                        ConsumeNumberTail(lexeme);
                        AddError(errors, tokenStart, "Invalid hexadecimal literal.");
                        return null;
                    }

                    return CreateToken(TokenType.HexLiteral, lexeme.ToString(), tokenStart);

                case LexerState.BinaryPrefix:
                    if (IsAtEnd() || !IsBinaryDigit(Peek()))
                    {
                        AddError(errors, tokenStart, "Invalid binary literal, expected at least one binary digit after 0b.");
                        return null;
                    }

                    lexeme.Append(Advance());
                    state = LexerState.BinaryNumber;
                    continue;

                case LexerState.BinaryNumber:
                    if (IsAtEnd())
                    {
                        return CreateToken(TokenType.BinaryLiteral, lexeme.ToString(), tokenStart);
                    }

                    current = Peek();

                    if (IsBinaryDigit(current))
                    {
                        lexeme.Append(Advance());
                        continue;
                    }

                    if (IsDigit(current) || IsIdentifierStart(current))
                    {
                        lexeme.Append(Advance());
                        ConsumeNumberTail(lexeme);
                        AddError(errors, tokenStart, "Invalid binary literal.");
                        return null;
                    }

                    return CreateToken(TokenType.BinaryLiteral, lexeme.ToString(), tokenStart);

                case LexerState.Base64Prefix:
                    if (IsAtEnd() || !IsBase64Digit(Peek()))
                    {
                        AddError(errors, tokenStart, "Invalid base-64 literal, expected at least one base-64 digit after 0z.");
                        return null;
                    }

                    lexeme.Append(Advance());
                    state = LexerState.Base64Number;
                    continue;

                case LexerState.Base64Number:
                    if (IsAtEnd())
                    {
                        return CreateToken(TokenType.Base64Literal, lexeme.ToString(), tokenStart);
                    }

                    current = Peek();

                    if (IsBase64Digit(current))
                    {
                        lexeme.Append(Advance());
                        continue;
                    }

                    if (current == '=')
                    {
                        lexeme.Append(Advance());
                        state = LexerState.Base64Padding1;
                        continue;
                    }

                    if (current == '_')
                    {
                        lexeme.Append(Advance());
                        ConsumeBase64Tail(lexeme);
                        AddError(errors, tokenStart, "Invalid base-64 literal.");
                        return null;
                    }

                    return CreateToken(TokenType.Base64Literal, lexeme.ToString(), tokenStart);

                case LexerState.Base64Padding1:
                    if (IsAtEnd())
                    {
                        return CreateToken(TokenType.Base64Literal, lexeme.ToString(), tokenStart);
                    }

                    current = Peek();

                    if (current == '=')
                    {
                        lexeme.Append(Advance());
                        state = LexerState.Base64Padding2;
                        continue;
                    }

                    if (IsBase64Digit(current) || current == '_')
                    {
                        lexeme.Append(Advance());
                        ConsumeBase64Tail(lexeme);
                        AddError(errors, tokenStart, "Invalid base-64 literal.");
                        return null;
                    }

                    return CreateToken(TokenType.Base64Literal, lexeme.ToString(), tokenStart);

                case LexerState.Base64Padding2:
                    if (IsAtEnd())
                    {
                        return CreateToken(TokenType.Base64Literal, lexeme.ToString(), tokenStart);
                    }

                    current = Peek();

                    if (IsBase64Digit(current) || current is '=' or '_')
                    {
                        lexeme.Append(Advance());
                        ConsumeBase64Tail(lexeme);
                        AddError(errors, tokenStart, "Invalid base-64 literal.");
                        return null;
                    }

                    return CreateToken(TokenType.Base64Literal, lexeme.ToString(), tokenStart);

                case LexerState.RealFraction:
                    if (IsAtEnd())
                    {
                        if (!hasFractionDigits)
                        {
                            AddError(errors, tokenStart, "Invalid real literal, expected digits after '.'.");
                            return null;
                        }

                        return CreateToken(TokenType.RealLiteral, lexeme.ToString(), tokenStart);
                    }

                    current = Peek();

                    if (IsDigit(current))
                    {
                        lexeme.Append(Advance());
                        hasFractionDigits = true;
                        continue;
                    }

                    if (!hasFractionDigits)
                    {
                        AddError(errors, tokenStart, "Invalid real literal, expected digits after '.'.");
                        return null;
                    }

                    if (current is 'e' or 'E')
                    {
                        lexeme.Append(Advance());
                        state = LexerState.RealExponentStart;
                        continue;
                    }

                    if (IsIdentifierStart(current))
                    {
                        lexeme.Append(Advance());
                        ConsumeNumberTail(lexeme);
                        AddError(errors, tokenStart, "Invalid number format.");
                        return null;
                    }

                    return CreateToken(TokenType.RealLiteral, lexeme.ToString(), tokenStart);

                case LexerState.RealExponentStart:
                    if (IsAtEnd())
                    {
                        AddError(errors, tokenStart, "Invalid real literal, expected exponent digits after e/E.");
                        return null;
                    }

                    current = Peek();

                    if (current is '+' or '-')
                    {
                        lexeme.Append(Advance());
                        state = LexerState.RealExponentSign;
                        continue;
                    }

                    if (IsDigit(current))
                    {
                        lexeme.Append(Advance());
                        state = LexerState.RealExponentDigits;
                        continue;
                    }

                    AddError(errors, tokenStart, "Invalid real literal, expected exponent digits after e/E.");
                    return null;

                case LexerState.RealExponentSign:
                    if (IsAtEnd() || !IsDigit(Peek()))
                    {
                        AddError(errors, tokenStart, "Invalid real literal, expected exponent digits after e/E.");
                        return null;
                    }

                    lexeme.Append(Advance());
                    state = LexerState.RealExponentDigits;
                    continue;

                case LexerState.RealExponentDigits:
                    if (IsAtEnd())
                    {
                        return CreateToken(TokenType.RealLiteral, lexeme.ToString(), tokenStart);
                    }

                    current = Peek();

                    if (IsDigit(current))
                    {
                        lexeme.Append(Advance());
                        continue;
                    }

                    if (IsIdentifierStart(current))
                    {
                        lexeme.Append(Advance());
                        ConsumeNumberTail(lexeme);
                        AddError(errors, tokenStart, "Invalid number format.");
                        return null;
                    }

                    return CreateToken(TokenType.RealLiteral, lexeme.ToString(), tokenStart);

                case LexerState.CharLiteral:
                    if (IsAtEnd() || Peek() == '\n')
                    {
                        AddError(errors, tokenStart, "Unclosed character literal.");
                        return null;
                    }

                    current = Peek();

                    if (current == '\\')
                    {
                        lexeme.Append(Advance());
                        state = LexerState.CharEscape;
                        continue;
                    }

                    if (current == '\'')
                    {
                        lexeme.Append(Advance());
                        AddError(errors, tokenStart, "Invalid character literal, empty value.");
                        return null;
                    }

                    lexeme.Append(Advance());
                    state = LexerState.CharEnd;
                    continue;

                case LexerState.CharEscape:
                    if (IsAtEnd() || Peek() == '\n')
                    {
                        AddError(errors, tokenStart, "Invalid character escape sequence.");
                        return null;
                    }

                    lexeme.Append(Advance());
                    state = LexerState.CharEnd;
                    continue;

                case LexerState.CharEnd:
                    if (IsAtEnd() || Peek() == '\n')
                    {
                        AddError(errors, tokenStart, "Unclosed character literal.");
                        return null;
                    }

                    if (Peek() == '\'')
                    {
                        lexeme.Append(Advance());
                        return CreateToken(TokenType.CharLiteral, lexeme.ToString(), tokenStart);
                    }

                    ConsumeCharTail(lexeme);
                    AddError(errors, tokenStart, "Invalid character literal, expected closing '\''.");
                    return null;

                case LexerState.StringLiteral:
                    if (IsAtEnd() || Peek() == '\n')
                    {
                        AddError(errors, tokenStart, "Unclosed string literal.");
                        return null;
                    }

                    current = Peek();

                    if (current == '"')
                    {
                        lexeme.Append(Advance());
                        return CreateToken(TokenType.StringLiteral, lexeme.ToString(), tokenStart);
                    }

                    if (current == '\\')
                    {
                        lexeme.Append(Advance());
                        state = LexerState.StringEscape;
                        continue;
                    }

                    lexeme.Append(Advance());
                    continue;

                case LexerState.StringEscape:
                    if (IsAtEnd() || Peek() == '\n')
                    {
                        AddError(errors, tokenStart, "Unclosed string literal.");
                        return null;
                    }

                    lexeme.Append(Advance());
                    state = LexerState.StringLiteral;
                    continue;

                case LexerState.Slash:
                    if (Match('/'))
                    {
                        lexeme.Clear();
                        state = LexerState.LineComment;
                        continue;
                    }

                    if (Match('*'))
                    {
                        lexeme.Clear();
                        state = LexerState.BlockComment;
                        continue;
                    }

                    return CreateToken(TokenType.Slash, lexeme.ToString(), tokenStart);

                case LexerState.LineComment:
                    if (IsAtEnd())
                    {
                        return null;
                    }

                    if (Peek() == '\n')
                    {
                        Advance();
                        state = LexerState.Start;
                        continue;
                    }

                    Advance();
                    continue;

                case LexerState.BlockComment:
                    if (IsAtEnd())
                    {
                        AddError(errors, tokenStart, "Unclosed multi-line comment.");
                        return null;
                    }

                    if (Peek() == '*')
                    {
                        Advance();
                        state = LexerState.BlockCommentStar;
                        continue;
                    }

                    Advance();
                    continue;

                case LexerState.BlockCommentStar:
                    if (IsAtEnd())
                    {
                        AddError(errors, tokenStart, "Unclosed multi-line comment.");
                        return null;
                    }

                    if (Peek() == '/')
                    {
                        Advance();
                        state = LexerState.Start;
                        continue;
                    }

                    if (Peek() == '*')
                    {
                        Advance();
                        continue;
                    }

                    Advance();
                    state = LexerState.BlockComment;
                    continue;

                case LexerState.Assign:
                    if (Match('='))
                    {
                        lexeme.Append('=');
                        return CreateToken(TokenType.Equal, lexeme.ToString(), tokenStart);
                    }

                    return CreateToken(TokenType.Assign, lexeme.ToString(), tokenStart);

                case LexerState.Not:
                    if (Match('='))
                    {
                        lexeme.Append('=');
                        return CreateToken(TokenType.NotEqual, lexeme.ToString(), tokenStart);
                    }

                    return CreateToken(TokenType.Not, lexeme.ToString(), tokenStart);

                case LexerState.Less:
                    if (Match('='))
                    {
                        lexeme.Append('=');
                        return CreateToken(TokenType.LessOrEqual, lexeme.ToString(), tokenStart);
                    }

                    return CreateToken(TokenType.Less, lexeme.ToString(), tokenStart);

                case LexerState.Greater:
                    if (Match('='))
                    {
                        lexeme.Append('=');
                        return CreateToken(TokenType.GreaterOrEqual, lexeme.ToString(), tokenStart);
                    }

                    return CreateToken(TokenType.Greater, lexeme.ToString(), tokenStart);

                case LexerState.And:
                    if (Match('&'))
                    {
                        lexeme.Append('&');
                        return CreateToken(TokenType.And, lexeme.ToString(), tokenStart);
                    }

                    AddError(errors, tokenStart, "Unexpected character '&'.");
                    return null;

                case LexerState.Or:
                    if (Match('|'))
                    {
                        lexeme.Append('|');
                        return CreateToken(TokenType.Or, lexeme.ToString(), tokenStart);
                    }

                    AddError(errors, tokenStart, "Unexpected character '|'.");
                    return null;

                default:
                    AddError(errors, tokenStart, "Internal lexer state error.");
                    return null;
            }
        }
    }

    private bool IsAtEnd()
    {
        return _index >= _source.Length;
    }

    private char Peek()
    {
        return IsAtEnd() ? '\0' : _source[_index];
    }

    private char Advance()
    {
        var current = _source[_index];
        _index++;

        if (current == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        return current;
    }

    private bool Match(char expected)
    {
        if (IsAtEnd() || Peek() != expected)
        {
            return false;
        }

        Advance();
        return true;
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd())
        {
            var current = Peek();
            if (current is ' ' or '\t' or '\r' or '\n')
            {
                Advance();
                continue;
            }

            break;
        }
    }

    private (int line, int column) CurrentPosition()
    {
        return (_line, _column);
    }

    private static Token CreateToken(TokenType type, string value, (int line, int column) position)
    {
        return new Token(type, value, position.line, position.column);
    }

    private static void AddError(ICollection<LexicalError> errors, (int line, int column) position, string message)
    {
        errors.Add(new LexicalError(message, position.line, position.column));
    }

    private void ConsumeNumberTail(StringBuilder lexeme)
    {
        while (!IsAtEnd() && IsNumberTailCharacter(Peek()))
        {
            lexeme.Append(Advance());
        }
    }

    private void ConsumeBase64Tail(StringBuilder lexeme)
    {
        while (!IsAtEnd() && (IsBase64Digit(Peek()) || Peek() is '=' or '_'))
        {
            lexeme.Append(Advance());
        }
    }

    private void ConsumeCharTail(StringBuilder lexeme)
    {
        while (!IsAtEnd() && Peek() != '\n')
        {
            var current = Advance();
            lexeme.Append(current);

            if (current == '\'')
            {
                return;
            }
        }
    }

    private static bool IsNumberTailCharacter(char character)
    {
        return IsDigit(character) || IsIdentifierStart(character) || character is '.' or '+' or '-';
    }

    private static bool IsIdentifierStart(char character)
    {
        return IsAsciiLetter(character) || character == '_';
    }

    private static bool IsIdentifierPart(char character)
    {
        return IsIdentifierStart(character) || IsDigit(character);
    }

    private static bool IsAsciiLetter(char character)
    {
        return character is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    }

    private static bool IsDigit(char character)
    {
        return character is >= '0' and <= '9';
    }

    private static bool IsOctalDigit(char character)
    {
        return character is >= '0' and <= '7';
    }

    private static bool IsBinaryDigit(char character)
    {
        return character is '0' or '1';
    }

    private static bool IsHexDigit(char character)
    {
        return IsDigit(character)
               || character is >= 'a' and <= 'f'
               || character is >= 'A' and <= 'F';
    }

    private static bool IsBase64Digit(char character)
    {
        return IsAsciiLetter(character)
               || IsDigit(character)
               || character is '+' or '/';
    }

    private static string DisplayCharacter(char character)
    {
        return character switch
        {
            '\0' => "<EOF>",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            _ => character.ToString()
        };
    }
}
