using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.UnitTests;

public class RtmDiagnosticTests
{
    [Fact]
    public void Constructor_WithAllParameters_SetsProperties()
    {
        // Arrange
        var range = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var severity = DiagnosticSeverity.Warning;
        var source = "test-source";
        var message = "Test message";

        // Act
        var diagnostic = new RtmDiagnostic(range, severity, source, message);

        // Assert
        Assert.Equal(range, diagnostic.Range);
        Assert.Equal(severity, diagnostic.Severity);
        Assert.Equal(source, diagnostic.Source);
        Assert.Equal(message, diagnostic.Message);
    }

    [Fact]
    public void Constructor_WithMinimalParameters_SetsProperties()
    {
        // Arrange
        var range = new TextRange(new TextPosition(0, 0), new TextPosition(0, 1));

        // Act
        var diagnostic = new RtmDiagnostic(range, DiagnosticSeverity.Error, "src", "msg");

        // Assert
        Assert.Equal(range, diagnostic.Range);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("src", diagnostic.Source);
        Assert.Equal("msg", diagnostic.Message);
    }

    [Fact]
    public void Record_IsImmutable()
    {
        // Arrange
        var range = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var diagnostic = new RtmDiagnostic(range, DiagnosticSeverity.Error, "test", "message");

        // Act - Try to create modified copy
        var modified = diagnostic with { Severity = DiagnosticSeverity.Warning };

        // Assert
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity); // Original unchanged
        Assert.Equal(DiagnosticSeverity.Warning, modified.Severity); // New instance has change
    }

    [Fact]
    public void Equals_SameDiagnostics_ReturnsTrue()
    {
        // Arrange
        var range = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var diagnostic1 = new RtmDiagnostic(range, DiagnosticSeverity.Error, "test", "message");
        var diagnostic2 = new RtmDiagnostic(range, DiagnosticSeverity.Error, "test", "message");

        // Act & Assert
        Assert.Equal(diagnostic1, diagnostic2);
    }

    [Fact]
    public void Equals_DifferentMessage_ReturnsFalse()
    {
        // Arrange
        var range = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var diagnostic1 = new RtmDiagnostic(range, DiagnosticSeverity.Error, "test", "message1");
        var diagnostic2 = new RtmDiagnostic(range, DiagnosticSeverity.Error, "test", "message2");

        // Act & Assert
        Assert.NotEqual(diagnostic1, diagnostic2);
    }

    [Fact]
    public void Equals_DifferentSeverity_ReturnsFalse()
    {
        // Arrange
        var range = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var diagnostic1 = new RtmDiagnostic(range, DiagnosticSeverity.Error, "test", "message");
        var diagnostic2 = new RtmDiagnostic(range, DiagnosticSeverity.Warning, "test", "message");

        // Act & Assert
        Assert.NotEqual(diagnostic1, diagnostic2);
    }

    [Fact]
    public void CanBeAddedToList()
    {
        // Arrange
        var diagnostics = new List<RtmDiagnostic>();
        var range1 = new TextRange(new TextPosition(1, 0), new TextPosition(1, 5));
        var range2 = new TextRange(new TextPosition(2, 0), new TextPosition(2, 5));

        // Act
        diagnostics.Add(new RtmDiagnostic(range1, DiagnosticSeverity.Error, "lexer", "Error 1"));
        diagnostics.Add(new RtmDiagnostic(range2, DiagnosticSeverity.Warning, "parser", "Warning 1"));

        // Assert
        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics[0].Severity);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostics[1].Severity);
    }

    [Theory]
    [InlineData(DiagnosticSeverity.Error)]
    [InlineData(DiagnosticSeverity.Warning)]
    [InlineData(DiagnosticSeverity.Information)]
    [InlineData(DiagnosticSeverity.Hint)]
    public void Constructor_WithDifferentSeverities_SetsCorrectly(DiagnosticSeverity severity)
    {
        // Arrange
        var range = new TextRange(new TextPosition(0, 0), new TextPosition(0, 1));

        // Act
        var diagnostic = new RtmDiagnostic(range, severity, "test", "message");

        // Assert
        Assert.Equal(severity, diagnostic.Severity);
    }

    [Fact]
    public void ToString_ReturnsReadableString()
    {
        // Arrange
        var range = new TextRange(new TextPosition(5, 10), new TextPosition(5, 15));
        var diagnostic = new RtmDiagnostic(range, DiagnosticSeverity.Error, "parser", "Syntax error");

        // Act
        var result = diagnostic.ToString();

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("parser", result);
        Assert.Contains("Syntax error", result);
    }
}
