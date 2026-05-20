using System;

namespace AtomCCompiler
{
    /// <summary>
    /// Application entry point.
    /// This class only validates the command line and delegates the real work to Compiler.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Starts the compiler front-end.
        /// The program expects exactly one argument: the path to an Atom C source file.
        /// </summary>
        /// <param name="args">Command-line arguments received from the shell.</param>
        /// <returns>0 on success, non-zero when an error occurs.</returns>
        private static int Main(string[] args)
        {
            // Keeping the interface strict makes the program easier to use in a lab setting.
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: AtomCCompiler <source-file>");
                return 1;
            }

            var compiler = new Compiler();
            return compiler.Run(args[0]);
        }
    }
}
