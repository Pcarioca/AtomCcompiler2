namespace AtomCCompiler
{
    /// <summary>
    /// Every token type that the Atom C lexer can emit.
    /// Keywords have their own token types so later compiler phases can recognize them easily.
    /// </summary>
    public enum TokenType
    {
        ID,
        BREAK,
        CHAR,
        DOUBLE,
        ELSE,
        FOR,
        IF,
        INT,
        RETURN,
        STRUCT,
        VOID,
        WHILE,
        CT_INT,
        CT_REAL,
        CT_CHAR,
        CT_STRING,
        COMMA,
        SEMICOLON,
        LPAR,
        RPAR,
        LBRACKET,
        RBRACKET,
        LACC,
        RACC,
        END,
        ADD,
        SUB,
        MUL,
        DIV,
        DOT,
        AND,
        OR,
        NOT,
        ASSIGN,
        EQUAL,
        NOTEQ,
        LESS,
        LESSEQ,
        GREATER,
        GREATEREQ
    }
}
