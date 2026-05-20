using AtomCcompiler.Compiler.Diagnostics;
using AtomCcompiler.Compiler.Lexer;
using AtomCcompiler.Compiler.Parser;

namespace AtomCcompiler.Compiler;

/// <summary>
/// This class orchestrates lexical analysis for Atom C source code.
/// </summary>
public sealed class AtomCCompiler
{
    /// <summary>
    /// This field stores the lexer instance.
    /// </summary>
    private readonly AtomCLexer _lexer = new();

    /// <summary>
    /// This field stores the parser factory dependency.
    /// The parser itself is created per compilation because it works on one token list at a time.
    /// </summary>
    private AtomCParser CreateParser(IReadOnlyList<Token> tokens) => new(tokens);

    /// <summary>
    /// This method compiles raw Atom C source text.
    /// </summary>
    public CompilationResult Compile(string sourceCode)
    {
        // This line normalizes line endings and splits the source by line as requested.
        var lines = sourceCode.Replace("\r\n", "\n").Split('\n');

        // This line runs lexical analysis on the source lines.
        var lexerResult = _lexer.Tokenize(lines);

        // This list collects lexical diagnostics.
        var allErrors = new List<CompilerError>();

        // This line adds lexical errors to the final diagnostic list.
        allErrors.AddRange(lexerResult.Errors);

        // This branch stops the pipeline after lexical analysis when lexical errors already exist.
        if (lexerResult.Errors.Count > 0)
        {
            return new CompilationResult(lexerResult.Tokens, allErrors);
        }

        // This parser instance validates the token stream only after lexical analysis succeeds.
        var parser = CreateParser(lexerResult.Tokens);

        // This line runs syntax analysis on the lexer output.
        var parserResult = parser.Parse();

        // This line adds syntax diagnostics to the final compiler result.
        allErrors.AddRange(parserResult.Errors);

        // This line returns a complete compilation result.
        return new CompilationResult(lexerResult.Tokens, allErrors);
    }
}
