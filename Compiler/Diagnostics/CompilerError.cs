namespace AtomCcompiler.Compiler.Diagnostics;

/// <summary>
/// This base class represents a compiler diagnostic error.
/// </summary>
public abstract class CompilerError
{
    /// <summary>
    /// This constructor initializes the common error data.
    /// </summary>
    protected CompilerError(string message, int line, int column)
    {
        // This line stores the diagnostic message.
        Message = message;

        // This line stores the 1-based source line where the error occurred.
        Line = line;

        // This line stores the 1-based source column where the error occurred.
        Column = column;
    }

    /// <summary>
    /// This property holds the readable error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// This property holds the source line for the error.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// This property holds the source column for the error.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// This method returns a consistent printable format for errors.
    /// </summary>
    public override string ToString()
    {
        // This return statement creates a message like: "Error on line 2, column 10: expected ';'".
        return $"Error on line {Line}, column {Column}: {Message}";
    }
}
