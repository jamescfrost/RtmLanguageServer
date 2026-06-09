using RtmLanguageServer.Core.Parsing;
using Xunit;

namespace RtmLanguageServer.Tests;

public sealed class DiagnosticTests
{
    [Fact]
    public void Valid_document_has_no_diagnostics()
    {
        const string validSource = """
                                   RTM
                                   $ENTRY TEST
                                   $DATA
                                   ID N5
                                   $PROG()
                                   RETURN()
                                   """;

        var document = RtmDocument.Parse("file:///test.rtm", validSource);

        Assert.Empty(document.Diagnostics);
    }

    [Fact]
    public void Invalid_syntax_produces_diagnostics()
    {
        const string invalidSource = """
                                     RTM
                                     $ENTRY TEST
                                     $DATA
                                     INVALID SYNTAX HERE
                                     $PROG()
                                     RETURN()
                                     """;

        var document = RtmDocument.Parse("file:///test.rtm", invalidSource);

        Assert.NotEmpty(document.Diagnostics);
    }

    [Fact]
    public void Diagnostic_messages_contain_line_and_column()
    {
        const string invalidSource = """
                                     RTM
                                     $ENTRY TEST
                                     $PROG()
                                     MISSINGEND
                                     """;

        var document = RtmDocument.Parse("file:///test.rtm", invalidSource);

        Assert.NotEmpty(document.Diagnostics);
        var diagnostic = document.Diagnostics[0];

        // Should have valid line and column positions
        Assert.True(diagnostic.Range.Start.Line >= 0);
        Assert.True(diagnostic.Range.Start.Character >= 0);
        Assert.NotEmpty(diagnostic.Message);
    }
}
