using System.Collections.Generic;

namespace AtomCCompiler
{
    /// <summary>
    /// Central place where Atom C keywords are mapped to token types.
    /// This keeps the lexer logic small and makes it easy to add new keywords later.
    /// </summary>
    public static class KeywordMap
    {
        /// <summary>
        /// Static keyword table used by the lexer after it reads an identifier.
        /// </summary>
        private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
        {
            ["break"] = TokenType.BREAK,
            ["char"] = TokenType.CHAR,
            ["double"] = TokenType.DOUBLE,
            ["else"] = TokenType.ELSE,
            ["for"] = TokenType.FOR,
            ["if"] = TokenType.IF,
            ["int"] = TokenType.INT,
            ["return"] = TokenType.RETURN,
            ["struct"] = TokenType.STRUCT,
            ["void"] = TokenType.VOID,
            ["while"] = TokenType.WHILE
        };

        /// <summary>
        /// Checks whether an identifier text is actually a keyword.
        /// </summary>
        /// <param name="text">Identifier text collected by the lexer.</param>
        /// <param name="tokenType">Receives the matching token type when found.</param>
        /// <returns>True when the text is a keyword, otherwise false.</returns>
        public static bool TryGetKeywordType(string text, out TokenType tokenType)
        {
            return Keywords.TryGetValue(text, out tokenType);
        }
    }
}
