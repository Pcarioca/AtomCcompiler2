namespace AtomCCompiler
{
    /// <summary>
    /// Immutable source location used by tokens and diagnostics.
    /// The compiler tracks positions using 1-based line and column numbers.
    /// </summary>
    public readonly struct SourcePosition
    {
        /// <summary>
        /// Creates a new position object.
        /// </summary>
        /// <param name="line">1-based source line number.</param>
        /// <param name="column">1-based source column number.</param>
        public SourcePosition(int line, int column)
        {
            Line = line;
            Column = column;
        }

        /// <summary>
        /// Gets the source line number.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Gets the source column number.
        /// </summary>
        public int Column { get; }
    }
}
