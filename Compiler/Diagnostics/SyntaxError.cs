namespace AtomCcompiler.Compiler.Diagnostics;

/// <summary>
/// This class represents a syntax analysis error.
/// </summary>
public sealed class SyntaxError : CompilerError
{
    /// <summary>
    /// This constructor passes syntax error data to the base class.
    /// </summary>
    public SyntaxError(string message, int line, int column)
        : base(message, line, column)
    {
        // This constructor intentionally has no extra logic.
    }
}
