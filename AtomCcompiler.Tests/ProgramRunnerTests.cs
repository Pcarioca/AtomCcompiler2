using System.Diagnostics;

namespace AtomCcompiler.Tests;

/// <summary>
/// These tests exercise the console runner as a process so argument handling and output formatting stay covered.
/// </summary>
public sealed class ProgramRunnerTests
{
    /// <summary>
    /// This test verifies that running without arguments automatically checks the local Examples folder.
    /// </summary>
    [Fact]
    public void NoArgumentsDefaultsToExamplesFolderAndPrintsSummary()
    {
        var result = RunProgram();

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("EXAMPLES CHECK", result.StandardOutput);
        Assert.Contains("Folder:", result.StandardOutput);
        Assert.Contains("[OK]", result.StandardOutput);
        Assert.Contains("[LEXICAL ERRORS]", result.StandardOutput);
        Assert.Contains("[SYNTAX ERRORS]", result.StandardOutput);
        Assert.Contains("0.c", result.StandardOutput);
        Assert.Contains("valid.c", result.StandardOutput);
        Assert.Contains("missing_semicolon.c", result.StandardOutput);
        Assert.Contains("Summary:", result.StandardOutput);
    }

    /// <summary>
    /// This test verifies that a single file still uses the detailed token and diagnostics output.
    /// </summary>
    [Fact]
    public void SingleFileModeStillPrintsDetailedOutput()
    {
        var projectRoot = FindProjectRoot();
        var filePath = Path.Combine(projectRoot, "Examples", "valid.c");

        var result = RunProgram(filePath);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("FILE:", result.StandardOutput);
        Assert.Contains("STATUS: OK", result.StandardOutput);
        Assert.Contains("TOKENS:", result.StandardOutput);
        Assert.Contains("DIAGNOSTICS:", result.StandardOutput);
        Assert.Contains("No errors.", result.StandardOutput);
    }

    /// <summary>
    /// This test verifies that folder mode marks bad files as failed and returns a non-zero exit code.
    /// </summary>
    [Fact]
    public void FolderModePrintsFailureKindsAndReturnsNonZeroWhenAnyFileFails()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"atomc-runner-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            File.WriteAllText(Path.Combine(tempFolder, "good.c"), "int main(){return 0;}");
            File.WriteAllText(Path.Combine(tempFolder, "lexical.c"), "int x=09;");
            File.WriteAllText(Path.Combine(tempFolder, "syntax.c"), "void main(){ int x = 10 }");

            var result = RunProgram(tempFolder);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("COMPILATION CHECK", result.StandardOutput);
            Assert.Contains("[OK]", result.StandardOutput);
            Assert.Contains("good.c", result.StandardOutput);
            Assert.Contains("[LEXICAL ERRORS]", result.StandardOutput);
            Assert.Contains("lexical.c", result.StandardOutput);
            Assert.Contains("Invalid octal literal", result.StandardOutput);
            Assert.Contains("[SYNTAX ERRORS]", result.StandardOutput);
            Assert.Contains("syntax.c", result.StandardOutput);
            Assert.Contains("expected ';' after variable definition", result.StandardOutput);
            Assert.Contains("Summary:", result.StandardOutput);
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    /// <summary>
    /// This test verifies that more than one argument still produces a usage error.
    /// </summary>
    [Fact]
    public void MultipleArgumentsPrintUsageAndReturnNonZero()
    {
        var result = RunProgram("first.c", "second.c");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Usage:", result.StandardOutput);
    }

    /// <summary>
    /// This helper starts the built console application and captures its output.
    /// </summary>
    private static ProcessResult RunProgram(params string[] arguments)
    {
        var projectRoot = FindProjectRoot();
        var executablePath = Path.Combine(projectRoot, "bin", "Debug", "net10.0", "AtomCcompiler.dll");

        Assert.True(File.Exists(executablePath), $"Expected compiled application at {executablePath}");

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = projectRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add(executablePath);

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);

        var standardOutput = process!.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }

    /// <summary>
    /// This helper locates the project root from the test output directory.
    /// </summary>
    private static string FindProjectRoot()
    {
        var directory = AppContext.BaseDirectory;

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "AtomCcompiler.csproj")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("Could not locate the AtomCcompiler project root.");
    }

    /// <summary>
    /// This record stores one captured process result.
    /// </summary>
    private readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
