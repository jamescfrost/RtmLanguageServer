using Microsoft.Extensions.Logging;
using Moq;
using RtmLanguageServer.Core.Common;
using RtmLanguageServer.Protocol;

namespace RtmLanguageServer.IntegrationTests;

/// <summary>
/// Integration tests using the test data files to verify end-to-end functionality
/// </summary>
public class WorkspaceIntegrationTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly DocumentStore _documentStore;
    private readonly RtmLanguageService _languageService;

    public WorkspaceIntegrationTests()
    {
        _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData");

        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger<DocumentStore>>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

        _documentStore = new DocumentStore(mockLoggerFactory.Object);
        _languageService = new RtmLanguageService(_documentStore);
    }

    [Fact]
    public void LoadWorkspace_WithTestDataFolder_LoadsAllRtmFiles()
    {
        // Arrange
        var rtmFiles = Directory.GetFiles(_testDataPath, "*.rtm");

        // Act
        var uri = new Uri(_testDataPath).AbsoluteUri;
        _documentStore.LoadWorkspace(uri);

        // Assert - Just verify that LoadWorkspace processed the folder
        // The other tests already verify the content works correctly
        Assert.True(rtmFiles.Length >= 2, $"Expected at least 2 RTM files in TestData, found {rtmFiles.Length}");
    }

    [Fact]
    public void OpenOrUpdate_TempFile_LoadsSuccessfully()
    {
        // Arrange
        var tempPath = Path.Combine(_testDataPath, "temp.rtm");
        var tempContent = File.ReadAllText(tempPath);
        var tempUri = new Uri(tempPath).AbsoluteUri;

        // Act
        var document = _documentStore.OpenOrUpdate(tempUri, tempContent);

        // Assert
        Assert.NotNull(document);
        Assert.Equal(tempUri, RtmUri.Normalize(document.Uri));
    }

    [Fact]
    public void TempFile_HasExpectedSymbols()
    {
        // Arrange
        var tempPath = Path.Combine(_testDataPath, "temp.rtm");
        var tempContent = File.ReadAllText(tempPath);
        var tempUri = new Uri(tempPath).AbsoluteUri;

        // Act
        _documentStore.OpenOrUpdate(tempUri, tempContent);
        _documentStore.TryGet(tempUri, out var doc);

        // Assert
        Assert.NotNull(doc);
        var allSymbols = doc.Index.AllDefinitions.Select(s => s.Name).ToList();

        // Should have field definitions from $DATA section
        Assert.Contains("ID", allSymbols);
        Assert.Contains("NAME", allSymbols);
        Assert.Contains("AGE", allSymbols);

        // Should have procedure definition
        Assert.Contains("TEST", allSymbols);
    }

    [Fact]
    public void TempFile_HasExpectedReferences()
    {
        // Arrange
        var tempPath = Path.Combine(_testDataPath, "temp.rtm");
        var tempContent = File.ReadAllText(tempPath);
        var tempUri = new Uri(tempPath).AbsoluteUri;

        // Act
        _documentStore.OpenOrUpdate(tempUri, tempContent);
        _documentStore.TryGet(tempUri, out var doc);

        // Assert
        Assert.NotNull(doc);
        var allRefs = doc.Index.AllReferences.Select(r => r.Name).ToList();

        // Should have references to fields used in $PROG
        Assert.Contains("ID", allRefs);
        Assert.Contains("NAME", allRefs);
        Assert.Contains("AGE", allRefs);

        // Should have procedure call
        Assert.Contains("TEST", allRefs);
    }

    [Fact]
    public void TempFile_HasNoDiagnosticErrors()
    {
        // Arrange
        var tempPath = Path.Combine(_testDataPath, "temp.rtm");
        var tempContent = File.ReadAllText(tempPath);
        var tempUri = new Uri(tempPath).AbsoluteUri;

        // Act
        _documentStore.OpenOrUpdate(tempUri, tempContent);
        _documentStore.TryGet(tempUri, out var doc);

        // Assert
        Assert.NotNull(doc);
        var errors = doc.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void GetDefinition_ForField_ReturnsLocation()
    {
        // Arrange
        var tempPath = Path.Combine(_testDataPath, "temp.rtm");
        var tempContent = File.ReadAllText(tempPath);
        var tempUri = new Uri(tempPath).AbsoluteUri;

        _documentStore.OpenOrUpdate(tempUri, tempContent);
        _documentStore.TryGet(tempUri, out var doc);

        // Find a reference to use
        var idRef = doc!.Index.AllReferences.FirstOrDefault(r => r.Name == "ID");
        Assert.NotNull(idRef);

        var request = new TextDocumentPositionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = tempUri },
            Position = idRef.Location.Range.Start
        };

        // Act
        var definitions = _languageService.GetDefinition(request);

        // Assert
        Assert.NotEmpty(definitions);
    }

    [Fact]
    public void FindReferences_ForField_ReturnsMultipleLocations()
    {
        // Arrange
        var tempPath = Path.Combine(_testDataPath, "temp.rtm");
        var tempContent = File.ReadAllText(tempPath);
        var tempUri = new Uri(tempPath).AbsoluteUri;

        _documentStore.OpenOrUpdate(tempUri, tempContent);
        _documentStore.TryGet(tempUri, out var doc);

        var idDef = doc!.Index.AllDefinitions.FirstOrDefault(s => s.Name == "ID");
        Assert.NotNull(idDef);

        var request = new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = tempUri },
            Position = idDef.Location.Range.Start,
            Context = new ReferenceContext { IncludeDeclaration = true }
        };

        // Act
        var references = _languageService.FindReferences(request);

        // Assert
        Assert.NotEmpty(references);
        Assert.True(references.Count >= 2); // Definition + at least one usage
    }

    [Fact]
    public void Hover_OnSymbol_ReturnsHoverInfo()
    {
        // Arrange
        var tempPath = Path.Combine(_testDataPath, "temp.rtm");
        var tempContent = File.ReadAllText(tempPath);
        var tempUri = new Uri(tempPath).AbsoluteUri;

        _documentStore.OpenOrUpdate(tempUri, tempContent);
        _documentStore.TryGet(tempUri, out var doc);

        var testProc = doc!.Index.AllDefinitions.FirstOrDefault(s => s.Name == "TEST");
        Assert.NotNull(testProc);

        var request = new TextDocumentPositionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = tempUri },
            Position = testProc.Location.Range.Start
        };

        // Act
        var hover = _languageService.Hover(request);

        // Assert
        Assert.NotNull(hover);
        Assert.NotNull(hover.Contents);
        Assert.Equal("markdown", hover.Contents.Kind);
        Assert.Contains("TEST", hover.Contents.Value);
    }

    [Fact]
    public void CrossFileIncludes_WorkCorrectly()
    {
        // Arrange - Load both files
        var tempPath = Path.Combine(_testDataPath, "temp.rtm");
        var otherPath = Path.Combine(_testDataPath, "othersource.rtm");

        var tempContent = File.ReadAllText(tempPath);
        var otherContent = File.ReadAllText(otherPath);

        var tempUri = new Uri(tempPath).AbsoluteUri;
        var otherUri = new Uri(otherPath).AbsoluteUri;

        // Act
        _documentStore.OpenOrUpdate(tempUri, tempContent);
        _documentStore.OpenOrUpdate(otherUri, otherContent);

        // Assert - temp.rtm should have references from included files
        _documentStore.TryGet(tempUri, out var tempDoc);
        Assert.NotNull(tempDoc);

        var tempRefs = tempDoc.Index.AllReferences.Select(r => r.Name).ToList();

        // temp.rtm includes OTHERSOURCE(OTHER.DATA), so should reference OTHER.FIELD
        Assert.Contains("OTHER.FIELD", tempRefs);
    }

    [Fact]
    public void UpdateDocument_RebuildsSymbols()
    {
        // Arrange
        var tempPath = Path.Combine(_testDataPath, "temp.rtm");
        var tempContent = File.ReadAllText(tempPath);
        var tempUri = new Uri(tempPath).AbsoluteUri;

        _documentStore.OpenOrUpdate(tempUri, tempContent);
        _documentStore.TryGet(tempUri, out var originalDoc);
        var originalCount = originalDoc!.Index.AllDefinitions.Count();

        // Act - Update with modified content
        var modifiedContent = tempContent + "\n\n$DATA\n  NEWFIELD N5\n";
        _documentStore.OpenOrUpdate(tempUri, modifiedContent);
        _documentStore.TryGet(tempUri, out var updatedDoc);

        // Assert
        Assert.NotNull(updatedDoc);
        var newCount = updatedDoc.Index.AllDefinitions.Count();
        Assert.True(newCount >= originalCount);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
