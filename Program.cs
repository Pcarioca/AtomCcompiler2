using AtomCcompiler.Compiler;

// This class hosts the console entry point for the Atom C compiler demo.
internal static class Program
{
    // This constant stores the canonical project file name used to locate the repo root.
    private const string ProjectFileName = "AtomCcompiler.csproj";

    // This constant stores the default examples folder name.
    private const string ExamplesFolderName = "Examples";

    // This method is the program entry point.
    private static int Main(string[] args)
    {
        // This branch rejects unsupported argument counts while still allowing zero arguments.
        if (args.Length > 1)
        {
            // This line prints a clear usage message when arguments are incorrect.
            PrintUsage();

            // This return exits with a non-zero code.
            return 1;
        }

        // This branch defaults to the local Examples folder when no argument is provided.
        if (args.Length == 0)
        {
            var examplesPath = TryFindExamplesFolder();

            // This branch reports a clear error when the repo-local examples folder cannot be found.
            if (examplesPath is null)
            {
                Console.WriteLine("Error: could not locate the Examples folder.");
                return 1;
            }

            // This return runs the default examples check.
            return CheckFolder(examplesPath, "EXAMPLES CHECK");
        }

        // This variable stores the first CLI argument as source path.
        var inputPath = Path.GetFullPath(args[0]);

        // This branch checks an entire folder for lexical errors.
        if (Directory.Exists(inputPath))
        {
            return CheckFolder(inputPath, "COMPILATION CHECK");
        }

        // This branch handles one source file.
        if (File.Exists(inputPath))
        {
            return CheckSingleFile(inputPath);
        }

        // This line prints a clear error for missing file or folder input.
        Console.WriteLine($"Error: file or folder not found: {inputPath}");

        // This return exits with a non-zero code.
        return 1;
    }

    // This method prints detailed tokens and diagnostics for one file.
    private static int CheckSingleFile(string sourcePath)
    {
        // This line prints the analyzed file path before token output.
        Console.WriteLine($"FILE: {sourcePath}");
        Console.WriteLine();

        // This branch handles file read failures without crashing the whole program.
        if (!TryCompileFile(sourcePath, out var result, out var readError))
        {
            Console.WriteLine("STATUS: ERRORS");
            Console.WriteLine();
            Console.WriteLine("TOKENS:");
            Console.WriteLine();
            Console.WriteLine("DIAGNOSTICS:");
            Console.WriteLine($"  {readError}");
            return 1;
        }

        // This line prints a short compilation status before the detailed sections.
        Console.WriteLine($"STATUS: {GetStatusLabel(result)}");
        Console.WriteLine();

        // This line prints a section header for tokens.
        Console.WriteLine("TOKENS:");

        // This loop prints every token produced by the lexer.
        foreach (var token in result.Tokens)
        {
            // This line writes one token per line in a readable format.
            Console.WriteLine($"  {token}");
        }

        // This line prints a section header for diagnostics.
        Console.WriteLine();
        Console.WriteLine("DIAGNOSTICS:");

        // This line prints grouped diagnostics so the user can immediately see whether the failure is lexical or syntactic.
        PrintDiagnostics(result, "  ");

        // This return uses a non-zero exit code when either lexical or syntax analysis fails.
        return result.Success ? 0 : 1;
    }

    // This method checks every file in a folder and prints a readable pass/fail summary for each one.
    private static int CheckFolder(string folderPath, string header)
    {
        var sourceFiles = Directory.GetFiles(folderPath)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (sourceFiles.Length == 0)
        {
            Console.WriteLine($"No files found in folder: {folderPath}");
            return 0;
        }

        var okCount = 0;
        var failedCount = 0;

        Console.WriteLine(header);
        Console.WriteLine($"Folder: {folderPath}");
        Console.WriteLine();

        foreach (var sourceFile in sourceFiles)
        {
            var fileName = Path.GetFileName(sourceFile);

            // This branch reports file system problems for one file while continuing with the rest.
            if (!TryCompileFile(sourceFile, out var result, out var readError))
            {
                failedCount++;
                Console.WriteLine($"  [ERRORS] {fileName}");
                Console.WriteLine($"    {readError}");
                continue;
            }

            if (result.Errors.Count == 0)
            {
                okCount++;
                Console.WriteLine($"  [OK]     {fileName}");
                continue;
            }

            failedCount++;
            Console.WriteLine($"  [{GetStatusLabel(result)}] {fileName}");
            PrintDiagnostics(result, "    ");
        }

        Console.WriteLine();
        Console.WriteLine($"Summary: {sourceFiles.Length} files checked, {okCount} OK, {failedCount} FAILED");

        return failedCount > 0 ? 1 : 0;
    }

    // This method resolves the default repo-local Examples folder.
    private static string? TryFindExamplesFolder()
    {
        // This branch first tries the project root relative to the current working directory.
        var currentDirectoryRoot = TryFindProjectRoot(Environment.CurrentDirectory);
        var currentDirectoryExamples = TryBuildExamplesPath(currentDirectoryRoot);
        if (currentDirectoryExamples is not null)
        {
            return currentDirectoryExamples;
        }

        // This branch falls back to the application base directory so running from bin still works.
        var baseDirectoryRoot = TryFindProjectRoot(AppContext.BaseDirectory);
        return TryBuildExamplesPath(baseDirectoryRoot);
    }

    // This method walks upward from a starting directory until it finds the project file.
    private static string? TryFindProjectRoot(string startDirectory)
    {
        var directory = Path.GetFullPath(startDirectory);

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, ProjectFileName)))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName ?? string.Empty;
        }

        return null;
    }

    // This method converts a project root into an existing Examples folder path.
    private static string? TryBuildExamplesPath(string? projectRoot)
    {
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            return null;
        }

        var examplesPath = Path.Combine(projectRoot, ExamplesFolderName);
        return Directory.Exists(examplesPath) ? examplesPath : null;
    }

    // This method converts a compilation result into a short human-readable status label.
    private static string GetStatusLabel(CompilationResult result)
    {
        if (result.HasLexicalErrors && !result.HasSyntaxErrors)
        {
            return "LEXICAL ERRORS";
        }

        if (!result.HasLexicalErrors && result.HasSyntaxErrors)
        {
            return "SYNTAX ERRORS";
        }

        if (!result.HasLexicalErrors && !result.HasSyntaxErrors)
        {
            return "OK";
        }

        return "ERRORS";
    }

    // This method prints grouped diagnostics for lexical and syntax analysis.
    private static void PrintDiagnostics(CompilationResult result, string indent)
    {
        if (result.Success)
        {
            Console.WriteLine($"{indent}No errors.");
            return;
        }

        if (result.HasLexicalErrors)
        {
            Console.WriteLine($"{indent}LEXICAL ERRORS:");

            foreach (var error in result.LexicalErrors)
            {
                Console.WriteLine($"{indent}  {error}");
            }
        }

        if (result.HasSyntaxErrors)
        {
            Console.WriteLine($"{indent}SYNTAX ERRORS:");

            foreach (var error in result.SyntaxErrors)
            {
                Console.WriteLine($"{indent}  {error}");
            }
        }

        // This branch is a safety net for any future diagnostic type not covered by the current compiler phases.
        if (!result.HasLexicalErrors && !result.HasSyntaxErrors && result.Errors.Count > 0)
        {
            Console.WriteLine($"{indent}ERRORS:");

            foreach (var error in result.Errors)
            {
                Console.WriteLine($"{indent}  {error}");
            }
        }
    }

    // This method prints a consistent usage message for invalid command lines.
    private static void PrintUsage()
    {
        Console.WriteLine("Usage: AtomCcompiler [path-to-atomc-file-or-folder]");
        Console.WriteLine("       When no path is provided, the local Examples folder is checked.");
    }

    // This method compiles one source file while converting file access failures into readable text.
    private static bool TryCompileFile(string sourcePath, out CompilationResult result, out string? errorMessage)
    {
        try
        {
            result = CompileFile(sourcePath);
            errorMessage = null;
            return true;
        }
        catch (IOException exception)
        {
            result = new CompilationResult([], []);
            errorMessage = $"Could not read file: {exception.Message}";
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            result = new CompilationResult([], []);
            errorMessage = $"Cannot access file: {exception.Message}";
            return false;
        }
    }

    // This method compiles one source file.
    private static CompilationResult CompileFile(string sourcePath)
    {
        // This line reads the full Atom C source file.
        var sourceCode = File.ReadAllText(sourcePath);

        // This compiler instance runs lexical analysis.
        var compiler = new AtomCCompiler();

        // This return runs the compilation pipeline.
        return compiler.Compile(sourceCode);
    }
}
