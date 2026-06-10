using Antlr4.Runtime;
using RtmLanguageServer.Core.Common;
using RtmLanguageServer.Core.Parsing;

namespace RtmLanguageServer.Core.UnitTests;

public class CollectingErrorListenerTests
{
    [Fact]
    public void SyntaxError_AddsRtmDiagnosticToList()
    {
        // Arrange
        var diagnostics = new List<RtmDiagnostic>();
        var listener = new CollectingErrorListener<int>(diagnostics, "test-source");

        // Act
        listener.SyntaxError(
            TextWriter.Null,
            null!,
            42,
            line: 5,
            charPositionInLine: 10,
            msg: "Test error message",
            e: null!);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("Test error message", diagnostic.Message);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("test-source", diagnostic.Source);
        Assert.Equal(4, diagnostic.Range.Start.Line); // 5 - 1
        Assert.Equal(10, diagnostic.Range.Start.Character);
        Assert.Equal(4, diagnostic.Range.End.Line);
        Assert.Equal(11, diagnostic.Range.End.Character); // 10 + 1
    }

    [Fact]
    public void SyntaxError_MultipleErrors_AddsAllDiagnostics()
    {
        // Arrange
        var diagnostics = new List<RtmDiagnostic>();
        var listener = new CollectingErrorListener<IToken>(diagnostics, "parser");

        // Act
        listener.SyntaxError(TextWriter.Null, null!, null!, 1, 0, "Error 1", null!);
        listener.SyntaxError(TextWriter.Null, null!, null!, 2, 5, "Error 2", null!);
        listener.SyntaxError(TextWriter.Null, null!, null!, 3, 10, "Error 3", null!);

        // Assert
        Assert.Equal(3, diagnostics.Count);
        Assert.Equal("Error 1", diagnostics[0].Message);
        Assert.Equal("Error 2", diagnostics[1].Message);
        Assert.Equal("Error 3", diagnostics[2].Message);
    }

    [Fact]
    public void Constructor_DefaultSource_UsesAntlr()
    {
        // Arrange
        var diagnostics = new List<RtmDiagnostic>();
        var listener = new CollectingErrorListener<int>(diagnostics);

        // Act
        listener.SyntaxError(TextWriter.Null, null!, 0, 1, 0, "Test", null!);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("antlr", diagnostic.Source);
    }

    [Fact]
    public void SyntaxError_SourceIsLowercase()
    {
        // Arrange
        var diagnostics = new List<RtmDiagnostic>();
        var listener = new CollectingErrorListener<int>(diagnostics, "LEXER");

        // Act
        listener.SyntaxError(TextWriter.Null, null!, 0, 1, 0, "Test", null!);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("lexer", diagnostic.Source);
    }

    [Fact]
    public void SyntaxError_ConvertsLineNumberToZeroBased()
    {
        // Arrange
        var diagnostics = new List<RtmDiagnostic>();
        var listener = new CollectingErrorListener<int>(diagnostics);

        // Act
        listener.SyntaxError(TextWriter.Null, null!, 0, line: 10, charPositionInLine: 5, "Test", null!);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(9, diagnostic.Range.Start.Line); // 10 - 1
        Assert.Equal(5, diagnostic.Range.Start.Character);
    }

    [Fact]
    public void SyntaxError_RangeSpansSingleCharacter()
    {
        // Arrange
        var diagnostics = new List<RtmDiagnostic>();
        var listener = new CollectingErrorListener<int>(diagnostics);

        // Act
        listener.SyntaxError(TextWriter.Null, null!, 0, line: 5, charPositionInLine: 15, "Test", null!);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(15, diagnostic.Range.Start.Character);
        Assert.Equal(16, diagnostic.Range.End.Character); // +1
        Assert.Equal(diagnostic.Range.Start.Line, diagnostic.Range.End.Line);
    }

    [Fact]
    public void CanCollectLexerErrors()
    {
        // Arrange
        var diagnostics = new List<RtmDiagnostic>();
        var listener = new CollectingErrorListener<int>(diagnostics, "Lexer");

        // Act - Simulate lexer error with int symbol
        listener.SyntaxError(TextWriter.Null, null!, 65, 1, 0, "Unexpected character", null!);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("lexer", diagnostic.Source);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void CanCollectParserErrors()
    {
        // Arrange
        var diagnostics = new List<RtmDiagnostic>();
        var listener = new CollectingErrorListener<IToken>(diagnostics, "Parser");

        // Act - Simulate parser error with IToken symbol
        listener.SyntaxError(TextWriter.Null, null!, null!, 1, 0, "Missing semicolon", null!);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("parser", diagnostic.Source);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }
}
