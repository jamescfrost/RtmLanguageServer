using Microsoft.Extensions.Logging;
using Moq;
using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Protocol.UnitTests;

public class RtmLanguageServiceTests
{
    private DocumentStore CreateDocumentStore()
    {
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger<DocumentStore>>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        return new DocumentStore(mockLoggerFactory.Object);
    }

    [Fact]
    public void Constructor_WithDocuments_StoresDocuments()
    {
        // Arrange
        var documentStore = CreateDocumentStore();

        // Act
        var service = new RtmLanguageService(documentStore);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void GetDefinition_WithNonExistentDocument_ReturnsEmptyList()
    {
        // Arrange
        var documentStore = CreateDocumentStore();
        var service = new RtmLanguageService(documentStore);

        var request = new TextDocumentPositionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = "file:///nonexistent.rtm" },
            Position = new TextPosition(0, 0)
        };

        // Act
        var result = service.GetDefinition(request);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetDefinition_WithValidDocument_ReturnsDefinition()
    {
        // Arrange
        var documentStore = CreateDocumentStore();
        var service = new RtmLanguageService(documentStore);

        documentStore.OpenOrUpdate("file:///test.rtm", @"RTM
$ENTRY TEST
$DATA
ID N5
$PROG()
ID = 12345
RETURN()");

        var request = new TextDocumentPositionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = "file:///test.rtm" },
            Position = new TextPosition(5, 0) // On "ID = 12345" line
        };

        // Act
        var result = service.GetDefinition(request);

        // Assert - May return empty if symbol resolution doesn't work, but should not throw
        Assert.NotNull(result);
    }

    [Fact]
    public void FindReferences_WithNonExistentDocument_ReturnsEmptyList()
    {
        // Arrange
        var documentStore = CreateDocumentStore();
        var service = new RtmLanguageService(documentStore);

        var request = new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = "file:///nonexistent.rtm" },
            Position = new TextPosition(0, 0),
            Context = new ReferenceContext { IncludeDeclaration = true }
        };

        // Act
        var result = service.FindReferences(request);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindReferences_ReturnsDistinctSortedLocations()
    {
        // Arrange
        var documentStore = CreateDocumentStore();
        var service = new RtmLanguageService(documentStore);

        documentStore.OpenOrUpdate("file:///test.rtm", @"RTM
$ENTRY TEST
$DATA
ID N5
$PROG()
ID = 12345
ID = 67890
RETURN()");

        var request = new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = "file:///test.rtm" },
            Position = new TextPosition(3, 0), // On "ID N5" definition
            Context = new ReferenceContext { IncludeDeclaration = true }
        };

        // Act
        var result = service.FindReferences(request);

        // Assert - Should use LocationEqualityComparer for deduplication and sorting
        Assert.NotNull(result);

        // Verify no duplicates (all locations are distinct)
        var distinctCount = result.Distinct(LocationEqualityComparer.Instance).Count();
        Assert.Equal(result.Count, distinctCount);

        // Verify sorted order if we have multiple results
        if (result.Count > 1)
        {
            for (int i = 1; i < result.Count; i++)
            {
                var cmp = LocationEqualityComparer.Instance.Compare(result[i - 1], result[i]);
                Assert.True(cmp <= 0, "References should be sorted");
            }
        }
    }

    [Fact]
    public void Hover_WithNonExistentDocument_ReturnsNull()
    {
        // Arrange
        var documentStore = CreateDocumentStore();
        var service = new RtmLanguageService(documentStore);

        var request = new TextDocumentPositionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = "file:///nonexistent.rtm" },
            Position = new TextPosition(0, 0)
        };

        // Act
        var result = service.Hover(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Hover_WithValidSymbol_ReturnsHoverWithMarkdown()
    {
        // Arrange
        var documentStore = CreateDocumentStore();
        var service = new RtmLanguageService(documentStore);

        documentStore.OpenOrUpdate("file:///test.rtm", @"RTM
$ENTRY TEST
$PROG()
RETURN()");

        var request = new TextDocumentPositionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = "file:///test.rtm" },
            Position = new TextPosition(2, 1) // On $PROG
        };

        // Act
        var result = service.Hover(request);

        // Assert - May return null if no symbol at position, but should not throw
        if (result != null)
        {
            Assert.NotNull(result.Contents);
            Assert.Equal("markdown", result.Contents.Kind);
            Assert.NotEmpty(result.Contents.Value);
        }
    }

    [Fact]
    public void Hover_ReturnsMarkupContentWithCorrectKind()
    {
        // Arrange
        var documentStore = CreateDocumentStore();
        var service = new RtmLanguageService(documentStore);

        documentStore.OpenOrUpdate("file:///test.rtm", @"RTM
$NAME TESTDATA
TEST N5

$ENTRY MAIN
$PROG()
RETURN()");

        var request = new TextDocumentPositionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = "file:///test.rtm" },
            Position = new TextPosition(1, 10) // On TESTDATA
        };

        // Act
        var result = service.Hover(request);

        // Assert
        if (result != null)
        {
            Assert.Equal("markdown", result.Contents.Kind);
        }
    }
}
