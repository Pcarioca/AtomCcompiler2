using AtomCcompiler.Compiler.Diagnostics;

namespace AtomCcompiler.Compiler;

/// <summary>
/// This class stores the full output of one compilation pass.
/// </summary>
public sealed class CompilationResult
{
    /// <summary>
    /// This constructor stores tokens and diagnostics from all compiler phases.
    /// </summary>
    public CompilationResult(IReadOnlyList<Token> tokens, IReadOnlyList<CompilerError> errors)
    {
        // This line stores all generated tokens.
        Tokens = tokens;

        // This line stores all lexical and syntax errors.
        Errors = errors;
    }

    /// <summary>
    /// This property exposes all generated tokens.
    /// </summary>
    public IReadOnlyList<Token> Tokens { get; }

    /// <summary>
    /// This property exposes all diagnostics.
    /// </summary>
    public IReadOnlyList<CompilerError> Errors { get; }

    /// <summary>
    /// This property indicates whether compilation completed without errors.
    /// </summary>
    public bool Success => Errors.Count == 0;

    /// <summary>
    /// This property exposes only lexical diagnostics from the full error list.
    /// </summary>
    public IReadOnlyList<LexicalError> LexicalErrors => Errors.OfType<LexicalError>().ToArray();

    /// <summary>
    /// This property exposes only syntax diagnostics from the full error list.
    /// </summary>
    public IReadOnlyList<SyntaxError> SyntaxErrors => Errors.OfType<SyntaxError>().ToArray();

    /// <summary>
    /// This property indicates whether lexical analysis failed.
    /// </summary>
    public bool HasLexicalErrors => LexicalErrors.Count > 0;

    /// <summary>
    /// This property indicates whether syntax analysis failed.
    /// </summary>
    public bool HasSyntaxErrors => SyntaxErrors.Count > 0;
}
