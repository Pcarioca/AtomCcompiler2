using AtomCcompiler.Compiler.Diagnostics;

namespace AtomCcompiler.Compiler.Parser;

/// <summary>
/// This class implements a simple recursive-descent parser for Atom C.
/// The parser validates syntax only and does not build an abstract syntax tree.
/// </summary>
public sealed partial class AtomCParser
{
    private static readonly HashSet<string> TypeKeywords =
    [
        "int",
        "double",
        "char",
        "float",
        "string",
        "bool"
    ];

    /// <summary>
    /// This field stores the token stream produced by the lexer.
    /// </summary>
    private readonly IReadOnlyList<Token> _tokens;

    /// <summary>
    /// This field stores the syntax diagnostics produced during parsing.
    /// </summary>
    private readonly List<SyntaxError> _errors = [];

    /// <summary>
    /// This field stores the current token index.
    /// </summary>
    private int _index;

    /// <summary>
    /// This constructor stores the token stream that must be parsed.
    /// </summary>
    public AtomCParser(IReadOnlyList<Token> tokens)
    {
        // This line stores the full token list for all parser rules.
        _tokens = tokens;
    }

    /// <summary>
    /// This property exposes the current token.
    /// </summary>
    private Token Current => Peek(0);

    /// <summary>
    /// This property exposes the previously consumed token.
    /// </summary>
    private Token Previous => Peek(-1);

    /// <summary>
    /// This method runs the full syntax analysis starting from the unit rule.
    /// </summary>
    public ParserResult Parse()
    {
        // This line attempts to parse the whole compilation unit.
        Unit();

        // This branch requires the logical end-of-file token when no prior syntax error exists.
        if (!HasErrors && !Expect(TokenType.EndOfFile, "expected end of file"))
        {
            // This block intentionally has no extra logic because Expect already reported the error.
        }

        // This return exposes all parser diagnostics to the compiler pipeline.
        return new ParserResult(_errors);
    }

    /// <summary>
    /// This method implements the unit grammar rule.
    /// A unit is a sequence of structure definitions, function definitions, or variable definitions.
    /// </summary>
    private bool Unit()
    {
        // This loop continues until the parser reaches the logical end of input or a fatal syntax error.
        while (!IsAtEnd() && !HasErrors)
        {

            /*struct Pt {
                int x;
            };

            int global;

            void main() {
            }*/
            if (StructDef())
            {
                continue;
            }

            if (FnDef())
            {
                continue;
            }

            if (VarDef())
            {
                continue;
            }

            AddError("expected structure definition, function definition, or variable definition");
            return false;
        }

        return !HasErrors;
    }
}
