using Microsoft.Extensions.Logging;
using Moq;

namespace RtmLanguageServer.Protocol.UnitTests;

public class DocumentStoreTests
{
    private DocumentStore CreateDocumentStore()
    {
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger<DocumentStore>>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        return new DocumentStore(mockLoggerFactory.Object);
    }

    [Fact]
    public void Constructor_WithLoggerFactory_CreatesLogger()
    {
        // Arrange
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger<DocumentStore>>();
        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(mockLogger.Object);

        // Act
        var documentStore = new DocumentStore(mockLoggerFactory.Object);

        // Assert
        mockLoggerFactory.Verify(x => x.CreateLogger(It.IsAny<string>()), Times.Once);
        Assert.NotNull(documentStore);
    }

    [Fact]
    public void Status_Event_CanBeSubscribed()
    {
        // Arrange
        var documentStore = CreateDocumentStore();
        var subscribed = false;

        // Act
        documentStore.Status += _ => subscribed = true;

        // Assert - Just verify no exception was thrown
        Assert.NotNull(documentStore);
    }

    [Fact]
    public void OpenOrUpdate_PublishesStatusEvents()
    {
        // Arrange
        var documentStore = CreateDocumentStore();
        var statuses = new List<WorkspaceStatus>();
        documentStore.Status += status => statuses.Add(status);

        // Act
        documentStore.OpenOrUpdate("file:///test.rtm", "RTM\n$ENTRY TEST\n$PROG()\nRETURN()");

        // Assert
        Assert.NotEmpty(statuses);
        Assert.Contains(statuses, s => s.Kind == StatusKind.Trace);
        Assert.Contains(statuses, s => s.Kind == StatusKind.Milestone);
    }

    [Fact]
    public void OpenOrUpdate_ReturnsDocument()
    {
        // Arrange
        var documentStore = CreateDocumentStore();

        // Act
        var document = documentStore.OpenOrUpdate("file:///test.rtm", "RTM\n$ENTRY TEST\n$PROG()\nRETURN()");

        // Assert
        Assert.NotNull(document);
        Assert.Contains("test.rtm", document.Uri.ToLowerInvariant());
    }

    [Fact]
    public void TryGet_WithExistingDocument_ReturnsTrue()
    {
        // Arrange
        var documentStore = CreateDocumentStore();
        documentStore.OpenOrUpdate("file:///test.rtm", "RTM\n$ENTRY TEST\n$PROG()\nRETURN()");

        // Act
        var result = documentStore.TryGet("file:///test.rtm", out var document);

        // Assert
        Assert.True(result);
        Assert.NotNull(document);
    }

    [Fact]
    public void TryGet_WithNonExistingDocument_ReturnsFalse()
    {
        // Arrange
        var documentStore = CreateDocumentStore();

        // Act
        var result = documentStore.TryGet("file:///nonexistent.rtm", out var document);

        // Assert
        Assert.False(result);
        Assert.Null(document);
    }

    [Fact]
    public void All_ReturnsAllDocuments()
    {
        // Arrange
        var documentStore = CreateDocumentStore();

        // Use unique URIs with full paths to avoid workspace auto-discovery
        var uri1 = "file:///D:/temp/unique_test1.rtm";
        var uri2 = "file:///D:/temp/unique_test2.rtm";

        documentStore.OpenOrUpdate(uri1, "RTM\n$ENTRY TEST1\n$PROG()\nRETURN()");
        documentStore.OpenOrUpdate(uri2, "RTM\n$ENTRY TEST2\n$PROG()\nRETURN()");

        // Act
        var all = documentStore.All.ToList();

        // Assert
        Assert.True(all.Count >= 2, $"Expected at least 2 documents, but found {all.Count}");
        Assert.Contains(all, d => d.Uri.Contains("unique_test1"));
        Assert.Contains(all, d => d.Uri.Contains("unique_test2"));
    }
}
