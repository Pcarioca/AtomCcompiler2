using AtomCcompiler.Compiler;
using AtomCcompiler.Compiler.Lexer;

namespace AtomCcompiler.Tests;

public sealed class LexerTests
{
    [Fact]
    public void KeywordsIncludeCourseAndExistingSuperset()
    {
        var result = LexOnly("double d; struct Pt { int x; } string s; bool b;");

        Assert.Empty(result.Errors);
        AssertToken(result.Tokens[0], TokenType.Keyword, "double");
        AssertToken(result.Tokens[3], TokenType.Keyword, "struct");
        AssertToken(result.Tokens[6], TokenType.Keyword, "int");
        Assert.Contains(result.Tokens, token => token.Type == TokenType.Keyword && token.Value == "string");
        Assert.Contains(result.Tokens, token => token.Type == TokenType.Keyword && token.Value == "bool");
    }

    [Fact]
    public void NumericLiteralsUseExpectedTokenKinds()
    {
        const string source = "int a=123; int z=0; double r=20E-1; int h=0x2A; int o=014; int b=0b1010; int s=0zAb+/==;";

        var result = LexOnly(source);

        Assert.Empty(result.Errors);
        Assert.Contains(result.Tokens, token => token.Type == TokenType.IntegerLiteral && token.Value == "123");
        Assert.Contains(result.Tokens, token => token.Type == TokenType.IntegerLiteral && token.Value == "0");
        Assert.Contains(result.Tokens, token => token.Type == TokenType.RealLiteral && token.Value == "20E-1");
        Assert.Contains(result.Tokens, token => token.Type == TokenType.HexLiteral && token.Value == "0x2A");
        Assert.Contains(result.Tokens, token => token.Type == TokenType.OctalLiteral && token.Value == "014");
        Assert.Contains(result.Tokens, token => token.Type == TokenType.BinaryLiteral && token.Value == "0b1010");
        Assert.Contains(result.Tokens, token => token.Type == TokenType.Base64Literal && token.Value == "0zAb+/==");
    }

    [Fact]
    public void CommentsAndEscapedLiteralsAreHandledByDfa()
    {
        const string source = "// ignored\nint x=1; /* block\n comment */ char c='\\n'; string s=\"a\\\"b\";";

        var result = LexOnly(source);

        Assert.Empty(result.Errors);
        Assert.DoesNotContain(result.Tokens, token => token.Value == "ignored" || token.Value == "block");
        Assert.Contains(result.Tokens, token => token.Type == TokenType.CharLiteral && token.Value == "'\\n'");
        Assert.Contains(result.Tokens, token => token.Type == TokenType.StringLiteral && token.Value == "\"a\\\"b\"");
    }

    [Theory]
    [InlineData("int x=0x;", "hexadecimal")]
    [InlineData("int x=09;", "octal")]
    [InlineData("int x=0b102;", "binary")]
    [InlineData("int x=0z;", "base-64")]
    [InlineData("int x=0zAb=+;", "base-64")]
    [InlineData("double x=1.;", "digits after")]
    [InlineData("double x=1e+;", "exponent digits")]
    public void InvalidNumericFormsAreSingleLexicalErrors(string source, string expectedMessagePart)
    {
        var result = LexOnly(source);

        var lexicalErrors = result.Errors.Where(error => error.Message.Contains(expectedMessagePart, StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Single(lexicalErrors);
        Assert.DoesNotContain(result.Tokens, token => token.Value is "0b10" or "0zAb=" or "1");
    }

    [Theory]
    [InlineData("char c='a;", "character")]
    [InlineData("string s=\"abc;", "string")]
    [InlineData("/* block", "multi-line comment")]
    [InlineData("int x=a & b;", "&")]
    [InlineData("int x=a | b;", "|")]
    public void UnterminatedLiteralsAndSingleLogicalOperatorsAreErrors(string source, string expectedMessagePart)
    {
        var result = LexOnly(source);

        Assert.Contains(result.Errors, error => error.Message.Contains(expectedMessagePart, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Tokens, token => token.Type is TokenType.And or TokenType.Or);
    }

    [Fact]
    public void ExistingExamplesStillTokenizeWithUpdatedLiteralAndKeywordClassification()
    {
        var projectRoot = FindProjectRoot();
        var exampleFiles = new[] { "5.c", "7.c", "8.c", "9.c" };

        foreach (var exampleFile in exampleFiles)
        {
            var source = File.ReadAllText(Path.Combine(projectRoot, "Examples", exampleFile));
            var result = LexOnly(source);

            Assert.Empty(result.Errors);
        }

        var example8 = LexOnly(File.ReadAllText(Path.Combine(projectRoot, "Examples", "8.c")));
        Assert.Contains(example8.Tokens, token => token.Type == TokenType.OctalLiteral && token.Value == "014");

        var example5 = LexOnly(File.ReadAllText(Path.Combine(projectRoot, "Examples", "5.c")));
        Assert.Contains(example5.Tokens, token => token.Type == TokenType.Keyword && token.Value == "double");

        var example9 = LexOnly(File.ReadAllText(Path.Combine(projectRoot, "Examples", "9.c")));
        Assert.Contains(example9.Tokens, token => token.Type == TokenType.Keyword && token.Value == "struct");
    }

    [Fact]
    public void ExampleFilesHaveExpectedLexicalDiagnostics()
    {
        var projectRoot = FindProjectRoot();
        var examplesPath = Path.Combine(projectRoot, "Examples");
        var exampleFiles = Directory.GetFiles(examplesPath).OrderBy(Path.GetFileName).ToArray();
        var lexicalFailureFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "0.c",
            "lexical_invalid_hex.c",
            "lexical_invalid_octal.c",
            "lexical_invalid_binary.c",
            "lexical_unclosed_comment.c"
        };

        Assert.NotEmpty(exampleFiles);

        foreach (var exampleFile in exampleFiles)
        {
            var source = File.ReadAllText(exampleFile);
            var result = LexOnly(source);
            var fileName = Path.GetFileName(exampleFile);

            Assert.Contains(result.Tokens, token => token.Type == TokenType.EndOfFile);

            if (lexicalFailureFiles.Contains(fileName))
            {
                Assert.NotEmpty(result.Errors);
                continue;
            }

            Assert.Empty(result.Errors);
        }
    }

    private static LexerResult LexOnly(string source)
    {
        var lines = source.Replace("\r\n", "\n").Split('\n');
        return new AtomCLexer().Tokenize(lines);
    }

    private static void AssertToken(Token token, TokenType expectedType, string expectedValue)
    {
        Assert.Equal(expectedType, token.Type);
        Assert.Equal(expectedValue, token.Value);
    }

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
