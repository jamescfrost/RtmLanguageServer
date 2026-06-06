using RtmLanguageServer.Analysis;
using RtmLanguageServer.Analysis.Symbols;
using Xunit;

namespace RtmLanguageServer.Tests;

public sealed class RtmAnalysisTests
{
    private const string Uri = "file:///test.rtm";

    private const string Source = """
RTM
$ENTRY TestOverlay
$PROG()
CalculateTax PROC()
ENDPROC
Main PROC()
    CalculateTax()
ENDPROC
""";

    [Fact]
    public void Parse_BuildsProcedureDefinitions()
    {
        var document = RtmDocument.Parse(Uri, Source);

        var procedures = document.Index
            .FindDefinitions("CalculateTax")
            .OfType<ProcedureSymbol>()
            .ToList();

        Assert.Single(procedures);
        Assert.Equal("CalculateTax", procedures[0].Name);
    }

    [Fact]
    public void Parse_AssignsProceduresToContainingOverlay()
    {
        var document = RtmDocument.Parse(Uri, Source);

        var overlay = Assert.Single(document.Index.FindDefinitions("TestOverlay").OfType<OverlaySymbol>());

        Assert.Contains(overlay.Children, child => child is ProcedureSymbol { Name: "CalculateTax" });
        Assert.Contains(overlay.Children, child => child is ProcedureSymbol { Name: "Main" });
    }

    [Fact]
    public void Parse_AddsFunctionCallsAsReferences()
    {
        var document = RtmDocument.Parse(Uri, Source);

        var references = document.Index.FindReferences("CalculateTax", includeDeclaration: false);

        Assert.Single(references);
        Assert.Equal(6, references[0].Range.Start.Line);
    }
}
