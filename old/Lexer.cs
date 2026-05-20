using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AtomCCompiler
{
    /// <summary>
    /// Character-by-character lexer for Atom C.
    /// The lexer is implemented as an explicit finite state machine so the control flow mirrors the DFA from class.
    /// </summary>
    public sealed class Lexer
    {
        /// <summary>
        /// Complete source text currently being tokenized.
        /// </summary>
        private readonly string _source;

        /// <summary>
        /// Current character index inside the source string.
        /// </summary>
        private int _index;

        /// <summary>
        /// Current 1-based line number.
        /// </summary>
        private int _line;

        /// <summary>
        /// Current 1-based column number.
        /// </summary>
        private int _column;

        /// <summary>
        /// Creates a lexer for a source string.
        /// </summary>
        /// <param name="source">Full Atom C source code.</param>
        public Lexer(string source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _index = 0;
            _line = 1;
            _column = 1;
        }

        /// <summary>
        /// Tokenizes the entire input and returns every token, including END.
        /// </summary>
        /// <returns>Read-only list of tokens generated from the source.</returns>
        public IReadOnlyList<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (true)
            {
                Token token = ReadNextToken();
                tokens.Add(token);

                if (token.Type == TokenType.END)
                {
                    break;
                }
            }

            return tokens;
        }

        /// <summary>
        /// Reads the next token using an explicit DFA.
        /// Every time the DFA reaches an accepting state, the method immediately creates and returns a Token object.
        /// </summary>
        /// <returns>The next token from the input stream.</returns>
        public Token ReadNextToken()
        {
            LexerState state = LexerState.Start;
            var lexeme = new StringBuilder();
            var processedText = new StringBuilder();
            SourcePosition tokenStart = CurrentPosition();
            bool hasFractionDigits = false;
            char charValue = '\0';
            bool hasCharValue = false;

            while (true)
            {
                switch (state)
                {
                    case LexerState.Start:
                        SkipWhitespace();
                        tokenStart = CurrentPosition();
                        lexeme.Clear();
                        processedText.Clear();
                        hasFractionDigits = false;
                        hasCharValue = false;

                        if (IsAtEnd())
                        {
                            return CreateToken(TokenType.END, string.Empty, tokenStart);
                        }

                        char current = Peek();

                        if (IsIdentifierStart(current))
                        {
                            lexeme.Append(Advance());
                            state = LexerState.Identifier;
                            continue;
                        }

                        if (IsDigit(current))
                        {
                            char digit = Advance();
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
                                return CreateToken(TokenType.ADD, Advance().ToString(), tokenStart);

                            case '-':
                                return CreateToken(TokenType.SUB, Advance().ToString(), tokenStart);

                            case '*':
                                return CreateToken(TokenType.MUL, Advance().ToString(), tokenStart);

                            case '(':
                                return CreateToken(TokenType.LPAR, Advance().ToString(), tokenStart);

                            case ')':
                                return CreateToken(TokenType.RPAR, Advance().ToString(), tokenStart);

                            case '[':
                                return CreateToken(TokenType.LBRACKET, Advance().ToString(), tokenStart);

                            case ']':
                                return CreateToken(TokenType.RBRACKET, Advance().ToString(), tokenStart);

                            case '{':
                                return CreateToken(TokenType.LACC, Advance().ToString(), tokenStart);

                            case '}':
                                return CreateToken(TokenType.RACC, Advance().ToString(), tokenStart);

                            case ';':
                                return CreateToken(TokenType.SEMICOLON, Advance().ToString(), tokenStart);

                            case ',':
                                return CreateToken(TokenType.COMMA, Advance().ToString(), tokenStart);

                            case '.':
                                return CreateToken(TokenType.DOT, Advance().ToString(), tokenStart);

                            default:
                                throw CreateError(tokenStart, $"invalid character '{DisplayCharacter(current)}'");
                        }

                    case LexerState.Identifier:
                        if (!IsAtEnd() && IsIdentifierPart(Peek()))
                        {
                            lexeme.Append(Advance());
                            continue;
                        }

                        string identifier = lexeme.ToString();

                        if (KeywordMap.TryGetKeywordType(identifier, out TokenType keywordType))
                        {
                            return CreateToken(keywordType, identifier, tokenStart);
                        }

                        return CreateToken(TokenType.ID, identifier, tokenStart, identifier);

                    case LexerState.Zero:
                        if (IsAtEnd())
                        {
                            return CreateIntegerToken(lexeme.ToString(), tokenStart);
                        }

                        current = Peek();

                        if (current == 'x' || current == 'X')
                        {
                            lexeme.Append(Advance());
                            state = LexerState.HexPrefix;
                            continue;
                        }

                        if (current == '.')
                        {
                            lexeme.Append(Advance());
                            state = LexerState.RealFraction;
                            continue;
                        }

                        if (current == 'e' || current == 'E')
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

                        if (current == '8' || current == '9')
                        {
                            throw CreateError(CurrentPosition(), $"invalid octal digit '{current}'");
                        }

                        if (IsIdentifierStart(current))
                        {
                            throw CreateError(CurrentPosition(), "invalid number format");
                        }

                        return CreateIntegerToken(lexeme.ToString(), tokenStart);

                    case LexerState.DecimalInteger:
                        if (IsAtEnd())
                        {
                            return CreateIntegerToken(lexeme.ToString(), tokenStart);
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

                        if (current == 'e' || current == 'E')
                        {
                            lexeme.Append(Advance());
                            state = LexerState.RealExponentStart;
                            continue;
                        }

                        if (IsIdentifierStart(current))
                        {
                            throw CreateError(CurrentPosition(), "invalid number format");
                        }

                        return CreateIntegerToken(lexeme.ToString(), tokenStart);

                    case LexerState.OctalInteger:
                        if (IsAtEnd())
                        {
                            return CreateIntegerToken(lexeme.ToString(), tokenStart);
                        }

                        current = Peek();

                        if (IsOctalDigit(current))
                        {
                            lexeme.Append(Advance());
                            continue;
                        }

                        if (current == '8' || current == '9')
                        {
                            throw CreateError(CurrentPosition(), $"invalid octal digit '{current}'");
                        }

                        if (current == '.')
                        {
                            lexeme.Append(Advance());
                            state = LexerState.RealFraction;
                            continue;
                        }

                        if (current == 'e' || current == 'E')
                        {
                            lexeme.Append(Advance());
                            state = LexerState.RealExponentStart;
                            continue;
                        }

                        if (IsIdentifierStart(current))
                        {
                            throw CreateError(CurrentPosition(), "invalid number format");
                        }

                        return CreateIntegerToken(lexeme.ToString(), tokenStart);

                    case LexerState.HexPrefix:
                        if (IsAtEnd())
                        {
                            throw CreateError(CurrentPosition(), "invalid hexadecimal digit");
                        }

                        current = Peek();

                        if (IsHexDigit(current))
                        {
                            lexeme.Append(Advance());
                            state = LexerState.HexNumber;
                            continue;
                        }

                        throw CreateError(CurrentPosition(), $"invalid hexadecimal digit '{DisplayCharacter(current)}'");

                    case LexerState.HexNumber:
                        if (IsAtEnd())
                        {
                            return CreateIntegerToken(lexeme.ToString(), tokenStart);
                        }

                        current = Peek();

                        if (IsHexDigit(current))
                        {
                            lexeme.Append(Advance());
                            continue;
                        }

                        if (IsIdentifierStart(current))
                        {
                            throw CreateError(CurrentPosition(), $"invalid hexadecimal digit '{DisplayCharacter(current)}'");
                        }

                        return CreateIntegerToken(lexeme.ToString(), tokenStart);

                    case LexerState.RealFraction:
                        if (IsAtEnd())
                        {
                            if (!hasFractionDigits)
                            {
                                throw CreateError(CurrentPosition(), "invalid real number format");
                            }

                            return CreateRealToken(lexeme.ToString(), tokenStart);
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
                            throw CreateError(CurrentPosition(), "invalid real number format");
                        }

                        if (current == 'e' || current == 'E')
                        {
                            lexeme.Append(Advance());
                            state = LexerState.RealExponentStart;
                            continue;
                        }

                        if (IsIdentifierStart(current))
                        {
                            throw CreateError(CurrentPosition(), "invalid number format");
                        }

                        return CreateRealToken(lexeme.ToString(), tokenStart);

                    case LexerState.RealExponentStart:
                        if (IsAtEnd())
                        {
                            throw CreateError(CurrentPosition(), "invalid exponent");
                        }

                        current = Peek();

                        if (current == '+' || current == '-')
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

                        throw CreateError(CurrentPosition(), "invalid exponent");

                    case LexerState.RealExponentSign:
                        if (IsAtEnd())
                        {
                            throw CreateError(CurrentPosition(), "invalid exponent");
                        }

                        current = Peek();

                        if (IsDigit(current))
                        {
                            lexeme.Append(Advance());
                            state = LexerState.RealExponentDigits;
                            continue;
                        }

                        throw CreateError(CurrentPosition(), "invalid exponent");

                    case LexerState.RealExponentDigits:
                        if (IsAtEnd())
                        {
                            return CreateRealToken(lexeme.ToString(), tokenStart);
                        }

                        current = Peek();

                        if (IsDigit(current))
                        {
                            lexeme.Append(Advance());
                            continue;
                        }

                        if (IsIdentifierStart(current))
                        {
                            throw CreateError(CurrentPosition(), "invalid number format");
                        }

                        return CreateRealToken(lexeme.ToString(), tokenStart);

                    case LexerState.CharLiteral:
                        if (IsAtEnd() || Peek() == '\n' || Peek() == '\r')
                        {
                            throw CreateError(tokenStart, "unclosed char literal");
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
                            throw CreateError(tokenStart, "malformed char literal");
                        }

                        charValue = Advance();
                        lexeme.Append(charValue);
                        hasCharValue = true;
                        state = LexerState.CharEnd;
                        continue;

                    case LexerState.CharEscape:
                        if (IsAtEnd() || Peek() == '\n' || Peek() == '\r')
                        {
                            throw CreateError(tokenStart, "unclosed char literal");
                        }

                        SourcePosition escapePosition = CurrentPosition();
                        current = Advance();
                        lexeme.Append(current);
                        charValue = DecodeEscape(current, escapePosition);
                        hasCharValue = true;
                        state = LexerState.CharEnd;
                        continue;

                    case LexerState.CharEnd:
                        if (IsAtEnd())
                        {
                            throw CreateError(tokenStart, "unclosed char literal");
                        }

                        if (Peek() != '\'')
                        {
                            throw CreateError(CurrentPosition(), "malformed char literal");
                        }

                        lexeme.Append(Advance());

                        if (!hasCharValue)
                        {
                            throw CreateError(tokenStart, "malformed char literal");
                        }

                        return CreateToken(TokenType.CT_CHAR, lexeme.ToString(), tokenStart, charValue);

                    case LexerState.StringLiteral:
                        if (IsAtEnd() || Peek() == '\n' || Peek() == '\r')
                        {
                            throw CreateError(tokenStart, "unclosed string literal");
                        }

                        current = Peek();

                        if (current == '"')
                        {
                            lexeme.Append(Advance());
                            return CreateToken(TokenType.CT_STRING, lexeme.ToString(), tokenStart, processedText.ToString());
                        }

                        if (current == '\\')
                        {
                            lexeme.Append(Advance());
                            state = LexerState.StringEscape;
                            continue;
                        }

                        processedText.Append(current);
                        lexeme.Append(Advance());
                        continue;

                    case LexerState.StringEscape:
                        if (IsAtEnd() || Peek() == '\n' || Peek() == '\r')
                        {
                            throw CreateError(tokenStart, "unclosed string literal");
                        }

                        escapePosition = CurrentPosition();
                        current = Advance();
                        lexeme.Append(current);
                        processedText.Append(DecodeEscape(current, escapePosition));
                        state = LexerState.StringLiteral;
                        continue;

                    case LexerState.Slash:
                        // After reading '/', the lexer must decide between division and comments.
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

                        return CreateToken(TokenType.DIV, lexeme.ToString(), tokenStart);

                    case LexerState.LineComment:
                        // Everything is ignored until the end of the current line.
                        if (IsAtEnd())
                        {
                            state = LexerState.Start;
                            continue;
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
                        // Inside a block comment, only '*' may lead to the closing sequence.
                        if (IsAtEnd())
                        {
                            throw CreateError(tokenStart, "unclosed block comment");
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
                        // We have already seen a '*', so '/' closes the comment and more '*' keeps us here.
                        if (IsAtEnd())
                        {
                            throw CreateError(tokenStart, "unclosed block comment");
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
                            return CreateToken(TokenType.EQUAL, lexeme.ToString(), tokenStart);
                        }

                        return CreateToken(TokenType.ASSIGN, lexeme.ToString(), tokenStart);

                    case LexerState.Not:
                        if (Match('='))
                        {
                            lexeme.Append('=');
                            return CreateToken(TokenType.NOTEQ, lexeme.ToString(), tokenStart);
                        }

                        return CreateToken(TokenType.NOT, lexeme.ToString(), tokenStart);

                    case LexerState.Less:
                        if (Match('='))
                        {
                            lexeme.Append('=');
                            return CreateToken(TokenType.LESSEQ, lexeme.ToString(), tokenStart);
                        }

                        return CreateToken(TokenType.LESS, lexeme.ToString(), tokenStart);

                    case LexerState.Greater:
                        if (Match('='))
                        {
                            lexeme.Append('=');
                            return CreateToken(TokenType.GREATEREQ, lexeme.ToString(), tokenStart);
                        }

                        return CreateToken(TokenType.GREATER, lexeme.ToString(), tokenStart);

                    case LexerState.And:
                        if (Match('&'))
                        {
                            lexeme.Append('&');
                            return CreateToken(TokenType.AND, lexeme.ToString(), tokenStart);
                        }

                        throw CreateError(tokenStart, "unexpected single '&'");

                    case LexerState.Or:
                        if (Match('|'))
                        {
                            lexeme.Append('|');
                            return CreateToken(TokenType.OR, lexeme.ToString(), tokenStart);
                        }

                        throw CreateError(tokenStart, "unexpected single '|'");

                    case LexerState.End:
                        return CreateToken(TokenType.END, string.Empty, tokenStart);

                    case LexerState.Error:
                        throw CreateError(tokenStart, "unknown lexical error");

                    default:
                        throw CreateError(tokenStart, "internal lexer state error");
                }
            }
        }

        /// <summary>
        /// Returns true when the lexer has consumed every source character.
        /// </summary>
        private bool IsAtEnd()
        {
            return _index >= _source.Length;
        }

        /// <summary>
        /// Returns the current character without consuming it.
        /// A '\0' sentinel is used when the source is exhausted.
        /// </summary>
        private char Peek()
        {
            return IsAtEnd() ? '\0' : _source[_index];
        }

        /// <summary>
        /// Returns the next character without consuming it.
        /// This helper is useful for look-ahead decisions.
        /// </summary>
        private char PeekNext()
        {
            return _index + 1 >= _source.Length ? '\0' : _source[_index + 1];
        }

        /// <summary>
        /// Consumes the current character and updates line/column counters.
        /// </summary>
        private char Advance()
        {
            char current = _source[_index];
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

        /// <summary>
        /// Consumes the current character only when it matches the expected value.
        /// </summary>
        /// <param name="expected">Character that should be present at the current position.</param>
        /// <returns>True when the character was consumed, otherwise false.</returns>
        private bool Match(char expected)
        {
            if (IsAtEnd() || Peek() != expected)
            {
                return false;
            }

            Advance();
            return true;
        }

        /// <summary>
        /// Skips spaces, tabs, carriage returns, and newlines between tokens.
        /// Comments are handled by the main state machine because they start with a real token candidate.
        /// </summary>
        private void SkipWhitespace()
        {
            while (!IsAtEnd())
            {
                char current = Peek();

                if (current == ' ' || current == '\t' || current == '\r' || current == '\n')
                {
                    Advance();
                    continue;
                }

                break;
            }
        }

        /// <summary>
        /// Returns the current source position as a value object.
        /// </summary>
        private SourcePosition CurrentPosition()
        {
            return new SourcePosition(_line, _column);
        }

        /// <summary>
        /// Builds a plain token without a parsed value.
        /// </summary>
        private static Token CreateToken(TokenType type, string lexeme, SourcePosition position, object? value = null)
        {
            return new Token(type, lexeme, position.Line, position.Column, value);
        }

        /// <summary>
        /// Builds an integer literal token and computes its numeric value after the lexeme is validated.
        /// </summary>
        private static Token CreateIntegerToken(string lexeme, SourcePosition position)
        {
            long value = ParseIntegerValue(lexeme);
            return CreateToken(TokenType.CT_INT, lexeme, position, value);
        }

        /// <summary>
        /// Builds a real literal token and computes its numeric value after the lexeme is validated.
        /// </summary>
        private static Token CreateRealToken(string lexeme, SourcePosition position)
        {
            double value = double.Parse(lexeme, CultureInfo.InvariantCulture);
            return CreateToken(TokenType.CT_REAL, lexeme, position, value);
        }

        /// <summary>
        /// Converts the current lexeme into an integer value.
        /// Decimal, octal-style, and hexadecimal forms are handled here.
        /// </summary>
        private static long ParseIntegerValue(string lexeme)
        {
            if (lexeme.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt64(lexeme.Substring(2), 16);
            }

            if (lexeme.Length > 1 && lexeme[0] == '0')
            {
                return Convert.ToInt64(lexeme, 8);
            }

            return long.Parse(lexeme, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Decodes the supported escape sequences used inside char and string literals.
        /// Unknown escapes are rejected because they likely indicate a mistake in the source program.
        /// </summary>
        private char DecodeEscape(char escapeCharacter, SourcePosition position)
        {
            switch (escapeCharacter)
            {
                case 'n':
                    return '\n';

                case 't':
                    return '\t';

                case '\\':
                    return '\\';

                case '"':
                    return '"';

                case '\'':
                    return '\'';

                default:
                    throw CreateError(position, $"invalid escape sequence '\\{DisplayCharacter(escapeCharacter)}'");
            }
        }

        /// <summary>
        /// Creates a lexer exception with a uniform error format.
        /// </summary>
        private static LexerException CreateError(SourcePosition position, string message)
        {
            return new LexerException(new CompilerError(position, message));
        }

        /// <summary>
        /// Checks whether a character can start an identifier.
        /// Atom C identifiers use ASCII letters and underscore.
        /// </summary>
        private static bool IsIdentifierStart(char character)
        {
            return IsAsciiLetter(character) || character == '_';
        }

        /// <summary>
        /// Checks whether a character can continue an identifier.
        /// </summary>
        private static bool IsIdentifierPart(char character)
        {
            return IsIdentifierStart(character) || IsDigit(character);
        }

        /// <summary>
        /// Checks whether a character is an ASCII letter.
        /// The lexer stays explicit and predictable by avoiding culture-sensitive classifications.
        /// </summary>
        private static bool IsAsciiLetter(char character)
        {
            return (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z');
        }

        /// <summary>
        /// Checks whether a character is a decimal digit.
        /// </summary>
        private static bool IsDigit(char character)
        {
            return character >= '0' && character <= '9';
        }

        /// <summary>
        /// Checks whether a character is valid in an octal-style integer.
        /// </summary>
        private static bool IsOctalDigit(char character)
        {
            return character >= '0' && character <= '7';
        }

        /// <summary>
        /// Checks whether a character is a valid hexadecimal digit.
        /// </summary>
        private static bool IsHexDigit(char character)
        {
            return IsDigit(character)
                || (character >= 'a' && character <= 'f')
                || (character >= 'A' && character <= 'F');
        }

        /// <summary>
        /// Converts special characters into readable fragments for diagnostics.
        /// </summary>
        private static string DisplayCharacter(char character)
        {
            return character switch
            {
                '\0' => "EOF",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ => character.ToString()
            };
        }
    }
}
