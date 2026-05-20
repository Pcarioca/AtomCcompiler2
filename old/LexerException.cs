using System;

namespace AtomCCompiler
{
    /// <summary>
    /// Exception used to stop lexing when a lexical error is found.
    /// The detailed message is carried by the CompilerError object.
    /// </summary>
    public sealed class LexerException : Exception
    {
        /// <summary>
        /// Creates a lexer exception from a compiler diagnostic.
        /// </summary>
        /// <param name="error">Diagnostic that explains the lexical problem.</param>
        public LexerException(CompilerError error)
            : base(error.ToString())
        {
            Error = error;
        }

        /// <summary>
        /// Gets the diagnostic associated with this exception.
        /// </summary>
        public CompilerError Error { get; }
    }
}
