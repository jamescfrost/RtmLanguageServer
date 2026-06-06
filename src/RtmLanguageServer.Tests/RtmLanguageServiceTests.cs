using RtmLanguageServer.Analysis.Core;
using RtmLanguageServer.Lsp;
using Xunit;

namespace RtmLanguageServer.Tests;

public sealed class RtmLanguageServiceTests
{
    private const string Uri = "file:///test.rtm";

    private const string Source = """
                                  RTM

                                  $ENTRY JAMES.TEST

                                  $DATA
                                    ID N5
                                    NAME A20
                                    AGE N3

                                  $PROG()
                                    ID = 12345
                                    NAME = "James"
                                    AGE = 30
                                    TEST(1)
                                  QUITZUG

                                  TEST PROC(ID)
                                    RETURN()
                                  ENDPROC
                                  """;

    [Fact]
    public void GetDefinition_FromFunctionCall_ReturnsProcedureDefinition()
    {
        var service = CreateService();

        var locations = service.GetDefinition(PositionAt(line: 13, character: 4));

        var location = Assert.Single(locations);
        Assert.Equal(Uri, location.Uri);
        Assert.Equal(16, location.Range.Start.Line);
    }

    [Fact]
    public void FindReferences_WithIncludeDeclaration_ReturnsDefinitionAndCallSite()
    {
        var service = CreateService();

        var locations = service.FindReferences(new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = Uri },
            Position = new TextPosition(6, 6),
            Context = new ReferenceContext { IncludeDeclaration = true }
        });

        Assert.Equal(2, locations.Count);
        Assert.Contains(locations, location => location.Range.Start.Line == 3);
        Assert.Contains(locations, location => location.Range.Start.Line == 6);
    }

    [Fact]
    public void Hover_FromFunctionCall_ReturnsProcedureAndOverlayContext()
    {
        var service = CreateService();

        var hover = service.Hover(PositionAt(line: 6, character: 6));

        Assert.NotNull(hover);
        Assert.Contains("procedure", hover!.Contents.Value, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CalculateTax", hover.Contents.Value);
        Assert.Contains("TestOverlay", hover.Contents.Value);
    }

    private static RtmLanguageService CreateService()
    {
        var documents = new DocumentStore();
        documents.OpenOrUpdate(Uri, Source);
        return new RtmLanguageService(documents);
    }

    private static TextDocumentPositionParams PositionAt(int line, int character) => new()
    {
        TextDocument = new TextDocumentIdentifier { Uri = Uri },
        Position = new TextPosition(line, character)
    };
}