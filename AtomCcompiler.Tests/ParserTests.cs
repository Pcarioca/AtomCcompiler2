using AtomCcompiler.Compiler;
using AtomCcompiler.Compiler.Diagnostics;

namespace AtomCcompiler.Tests;

/// <summary>
/// These tests focus on syntax analysis and on the lexer+parser pipeline working together.
/// </summary>
public sealed class ParserTests
{
    /// <summary>
    /// This test verifies that a minimal empty function parses successfully.
    /// </summary>
    [Fact]
    public void MinimalFunctionParsesSuccessfully()
    {
        var result = Compile("void main() { }");

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// This test verifies that the parser accepts supported variable definition forms.
    /// </summary>
    [Fact]
    public void VariableDefinitionsParseSuccessfully()
    {
        const string source = """
        int x;
        double y;
        char c;
        struct Pt p;
        int v[100];
        """;

        var result = Compile(source);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// This test verifies that function definitions with parameters parse successfully.
    /// </summary>
    [Fact]
    public void FunctionWithParametersParsesSuccessfully()
    {
        const string source = """
        int sum(int a, int b) {
            return a + b;
        }
        """;

        var result = Compile(source);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// This test verifies that if-else statements parse successfully.
    /// </summary>
    [Fact]
    public void IfElseStatementParsesSuccessfully()
    {
        const string source = """
        void main() {
            int x;
            if (x < 0) x = 0;
            else x = 1;
        }
        """;

        var result = Compile(source);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// This test verifies that while statements parse successfully.
    /// </summary>
    [Fact]
    public void WhileStatementParsesSuccessfully()
    {
        const string source = """
        void main() {
            int x;
            while (x < 10) x = x + 1;
        }
        """;

        var result = Compile(source);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// This test verifies that for statements parse successfully.
    /// </summary>
    [Fact]
    public void ForStatementParsesSuccessfully()
    {
        const string source = """
        void main() {
            int i;
            for (i = 0; i < 10; i = i + 1) {
            }
        }
        """;

        var result = Compile(source);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// This test verifies that structure definitions parse successfully.
    /// </summary>
    [Fact]
    public void StructDefinitionParsesSuccessfully()
    {
        const string source = """
        struct Pt {
            int x;
            int y;
        };
        """;

        var result = Compile(source);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// This test verifies that arrays and dot access parse successfully.
    /// </summary>
    [Fact]
    public void ArraysAndDotAccessParseSuccessfully()
    {
        const string source = """
        void main() {
            struct Pt points[10];
            int i;
            points[i].x = 2;
        }
        """;

        var result = Compile(source);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// This test verifies that function calls parse successfully.
    /// </summary>
    [Fact]
    public void FunctionCallsParseSuccessfully()
    {
        const string source = """
        void main() {
            put_s("hello");
            put_i(123);
        }
        """;

        var result = Compile(source);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// This test verifies that precedence-heavy expressions parse successfully.
    /// </summary>
    [Fact]
    public void ComplexExpressionParsesSuccessfully()
    {
        const string source = """
        void main() {
            int x;
            x = 1 + 2 * 3 < 10 && !0;
        }
        """;

        var result = Compile(source);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// This test verifies that the current repo examples intended as valid still pass syntax analysis.
    /// </summary>
    [Fact]
    public void CurrentValidExamplesParseSuccessfully()
    {
        var projectRoot = FindProjectRoot();
        var validExamples = new[] { "1.c", "2.c", "3.c", "5.c", "6.c", "7.c", "8.c", "9.c", "valid.c" };

        foreach (var fileName in validExamples)
        {
            var source = File.ReadAllText(Path.Combine(projectRoot, "Examples", fileName));
            var result = Compile(source);

            Assert.Empty(result.LexicalErrors);
            Assert.Empty(result.SyntaxErrors);
        }
    }

    /// <summary>
    /// This test verifies that the broken example files are classified as the intended failure types.
    /// </summary>
    [Fact]
    public void CurrentBrokenExamplesHaveExpectedFailureKinds()
    {
        var projectRoot = FindProjectRoot();
        var lexicalExamples = new[]
        {
            "0.c",
            "lexical_invalid_hex.c",
            "lexical_invalid_octal.c",
            "lexical_invalid_binary.c",
            "lexical_unclosed_comment.c"
        };

        foreach (var fileName in lexicalExamples)
        {
            var source = File.ReadAllText(Path.Combine(projectRoot, "Examples", fileName));
            var result = Compile(source);

            Assert.NotEmpty(result.LexicalErrors);
            Assert.Empty(result.SyntaxErrors);
        }

        var syntaxExamples = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["4.c"] = "identifier after type",
            ["missing_semicolon.c"] = "';' after variable definition",
            ["syntax_missing_paren.c"] = "')' after function parameters",
            ["syntax_missing_brace.c"] = "'}' after compound statement",
            ["syntax_invalid_assignment.c"] = "expression after '='",
            ["syntax_bad_field_access.c"] = "field name after '.'"
        };

        foreach (var pair in syntaxExamples)
        {
            var source = File.ReadAllText(Path.Combine(projectRoot, "Examples", pair.Key));
            var result = Compile(source);

            Assert.Empty(result.LexicalErrors);
            Assert.NotEmpty(result.SyntaxErrors);
            Assert.Contains(result.SyntaxErrors, error => error.Message.Contains(pair.Value, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// This test verifies that a missing semicolon produces a clear syntax diagnostic.
    /// </summary>
    [Fact]
    public void MissingSemicolonProducesSyntaxError()
    {
        var result = Compile("int x");

        Assert.Contains(result.SyntaxErrors, error => error.Message.Contains("';' after variable definition", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// This test verifies that a missing right parenthesis produces a clear syntax diagnostic.
    /// </summary>
    [Fact]
    public void MissingRightParenthesisProducesSyntaxError()
    {
        const string source = """
        void main( {
        }
        """;

        var result = Compile(source);

        Assert.Contains(result.SyntaxErrors, error => error.Message.Contains("')' after function parameters", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// This test verifies that a missing right brace produces a clear syntax diagnostic.
    /// </summary>
    [Fact]
    public void MissingRightBraceProducesSyntaxError()
    {
        const string source = """
        void main() {
            int x;
        """;

        var result = Compile(source);

        Assert.Contains(result.SyntaxErrors, error => error.Message.Contains("'}' after compound statement", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// This test verifies that an assignment missing the right-hand expression produces a clear syntax diagnostic.
    /// </summary>
    [Fact]
    public void InvalidAssignmentExpressionProducesSyntaxError()
    {
        const string source = """
        void main() {
            int x;
            x = ;
        }
        """;

        var result = Compile(source);

        Assert.Contains(result.SyntaxErrors, error => error.Message.Contains("expression after '='", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// This test verifies that a bad field access produces a clear syntax diagnostic.
    /// </summary>
    [Fact]
    public void BadFieldAccessProducesSyntaxError()
    {
        const string source = """
        void main() {
            p.;
        }
        """;

        var result = Compile(source);

        Assert.Contains(result.SyntaxErrors, error => error.Message.Contains("field name after '.'", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// This helper runs the full compiler pipeline so syntax tests cover lexer-to-parser integration.
    /// </summary>
    private static CompilationResult Compile(string source)
    {
        return new AtomCCompiler().Compile(source);
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
}
