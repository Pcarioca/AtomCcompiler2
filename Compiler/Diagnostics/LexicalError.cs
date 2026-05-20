namespace AtomCcompiler.Compiler.Diagnostics;

/// <summary>
/// This class represents a lexical analysis error.
/// </summary>
public sealed class LexicalError : CompilerError
{
    /// <summary>
    /// This constructor passes lexical error data to the base class.
    /// </summary>
    public LexicalError(string message, int line, int column)
        : base(message, line, column)
    {
        // This constructor intentionally has no extra logic.
    }
}
