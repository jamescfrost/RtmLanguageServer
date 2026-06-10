using RtmLanguageServer.Core.Analysis;
using RtmLanguageServer.Core.Common;
using RtmLanguageServer.Core.Symbols;

namespace RtmLanguageServer.Core.UnitTests;

public class SymbolIndexTests
{
    [Fact]
    public void AddDefinition_AddsSymbolToIndex()
    {
        // Arrange
        var index = new SymbolIndex();
        var symbol = new FieldSymbol("TestField", new Location("file:///test.rtm", new TextRange(new TextPosition(1, 0), new TextPosition(1, 9))));

        // Act
        index.AddDefinition(symbol);

        // Assert
        var definitions = index.FindDefinitions("TestField");
        Assert.Single(definitions);
        Assert.Equal("TestField", definitions[0].Name);
    }

    [Fact]
    public void AddDefinition_WithSameName_AddsMultipleDefinitions()
    {
        // Arrange
        var index = new SymbolIndex();
        var symbol1 = new FieldSymbol("Field", new Location("file:///test1.rtm", new TextRange(new TextPosition(1, 0), new TextPosition(1, 5))));
        var symbol2 = new FieldSymbol("Field", new Location("file:///test2.rtm", new TextRange(new TextPosition(1, 0), new TextPosition(1, 5))));

        // Act
        index.AddDefinition(symbol1);
        index.AddDefinition(symbol2);

        // Assert
        var definitions = index.FindDefinitions("Field");
        Assert.Equal(2, definitions.Count);
    }

    [Fact]
    public void FindDefinitions_WithNonExistentName_ReturnsEmpty()
    {
        // Arrange
        var index = new SymbolIndex();

        // Act
        var definitions = index.FindDefinitions("NonExistent");

        // Assert
        Assert.Empty(definitions);
    }

    [Fact]
    public void FindDefinitions_IsCaseInsensitive()
    {
        // Arrange
        var index = new SymbolIndex();
        var symbol = new FieldSymbol("TestField", new Location("file:///test.rtm", new TextRange(new TextPosition(1, 0), new TextPosition(1, 9))));
        index.AddDefinition(symbol);

        // Act
        var definitions1 = index.FindDefinitions("TESTFIELD");
        var definitions2 = index.FindDefinitions("testfield");

        // Assert
        Assert.Single(definitions1);
        Assert.Single(definitions2);
    }

    [Fact]
    public void AddReference_AddsReferenceToIndex()
    {
        // Arrange
        var index = new SymbolIndex();
        var reference = new FieldReference
        {
            Name = "TestField",
            Location = new Location("file:///test.rtm", new TextRange(new TextPosition(5, 0), new TextPosition(5, 9)))
        };

        // Act
        index.AddReference(reference);

        // Assert
        Assert.Contains(reference, index.AllReferences);
    }

    [Fact]
    public void AddReference_DuplicateLocation_OnlyAddsOnce()
    {
        // Arrange
        var index = new SymbolIndex();
        var location = new Location("file:///test.rtm", new TextRange(new TextPosition(5, 0), new TextPosition(5, 9)));
        var reference1 = new FieldReference { Name = "Field", Location = location };
        var reference2 = new FieldReference { Name = "Field", Location = location };

        // Act
        index.AddReference(reference1);
        index.AddReference(reference2);

        // Assert
        Assert.Single(index.AllReferences);
    }

    [Fact]
    public void FindReferences_WithMatchingSymbol_ReturnsReferences()
    {
        // Arrange
        var index = new SymbolIndex();
        var symbol = new FieldSymbol("TestField", new Location("file:///test.rtm", new TextRange(new TextPosition(1, 0), new TextPosition(1, 9))));
        var reference = new FieldReference
        {
            Name = "TestField",
            Location = new Location("file:///test.rtm", new TextRange(new TextPosition(5, 0), new TextPosition(5, 9)))
        };

        index.AddDefinition(symbol);
        index.AddReference(reference);

        // Act
        var references = index.FindReferences(symbol, includeDeclaration: false);

        // Assert
        Assert.Single(references);
        Assert.Equal(reference.Location, references[0]);
    }

    [Fact]
    public void FindReferences_IncludeDeclaration_IncludesSymbolLocation()
    {
        // Arrange
        var index = new SymbolIndex();
        var symbol = new FieldSymbol("TestField", new Location("file:///test.rtm", new TextRange(new TextPosition(1, 0), new TextPosition(1, 9))));
        var reference = new FieldReference
        {
            Name = "TestField",
            Location = new Location("file:///test.rtm", new TextRange(new TextPosition(5, 0), new TextPosition(5, 9)))
        };

        index.AddDefinition(symbol);
        index.AddReference(reference);

        // Act
        var references = index.FindReferences(symbol, includeDeclaration: true);

        // Assert
        Assert.Equal(2, references.Count);
        Assert.Contains(symbol.Location, references);
        Assert.Contains(reference.Location, references);
    }

    [Fact]
    public void FindReferences_WithoutDeclaration_ExcludesSymbolLocation()
    {
        // Arrange
        var index = new SymbolIndex();
        var symbol = new FieldSymbol("TestField", new Location("file:///test.rtm", new TextRange(new TextPosition(1, 0), new TextPosition(1, 9))));

        index.AddDefinition(symbol);

        // Act
        var references = index.FindReferences(symbol, includeDeclaration: false);

        // Assert
        Assert.Empty(references);
    }

    [Fact]
    public void FindReferences_RemovesDuplicates()
    {
        // Arrange
        var index = new SymbolIndex();
        var symbol = new FieldSymbol("TestField", new Location("file:///test.rtm", new TextRange(new TextPosition(1, 0), new TextPosition(1, 9))));
        var location = new Location("file:///test.rtm", new TextRange(new TextPosition(5, 0), new TextPosition(5, 9)));
        var reference1 = new FieldReference { Name = "TestField", Location = location };
        var reference2 = new FieldReference { Name = "TestField", Location = location };

        index.AddDefinition(symbol);
        index.AddReference(reference1);
        index.AddReference(reference2);

        // Act
        var references = index.FindReferences(symbol, includeDeclaration: false);

        // Assert
        Assert.Single(references);
    }

    [Fact]
    public void DefinitionAt_WithMatchingPosition_ReturnsSymbol()
    {
        // Arrange
        var index = new SymbolIndex();
        var symbol = new FieldSymbol("TestField", new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 14))));
        index.AddDefinition(symbol);

        // Act
        var result = index.DefinitionAt(new TextPosition(1, 7)); // Within range

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestField", result.Name);
    }

    [Fact]
    public void DefinitionAt_WithNonMatchingPosition_ReturnsNull()
    {
        // Arrange
        var index = new SymbolIndex();
        var symbol = new FieldSymbol("TestField", new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 14))));
        index.AddDefinition(symbol);

        // Act
        var result = index.DefinitionAt(new TextPosition(2, 0)); // Different line

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ReferenceAt_WithMatchingPosition_ReturnsReference()
    {
        // Arrange
        var index = new SymbolIndex();
        var reference = new FieldReference
        {
            Name = "TestField",
            Location = new Location("file:///test.rtm", new TextRange(new TextPosition(5, 0), new TextPosition(5, 9)))
        };
        index.AddReference(reference);

        // Act
        var result = index.ReferenceAt(new TextPosition(5, 4)); // Within range

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestField", result.Name);
    }

    [Fact]
    public void ReferenceAt_WithNonMatchingPosition_ReturnsNull()
    {
        // Arrange
        var index = new SymbolIndex();
        var reference = new FieldReference
        {
            Name = "TestField",
            Location = new Location("file:///test.rtm", new TextRange(new TextPosition(5, 0), new TextPosition(5, 9)))
        };
        index.AddReference(reference);

        // Act
        var result = index.ReferenceAt(new TextPosition(6, 0)); // Different line

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AllDefinitions_ReturnsAllAddedDefinitions()
    {
        // Arrange
        var index = new SymbolIndex();
        var symbol1 = new FieldSymbol("Field1", new Location("file:///test.rtm", new TextRange(new TextPosition(1, 0), new TextPosition(1, 6))));
        var symbol2 = new FieldSymbol("Field2", new Location("file:///test.rtm", new TextRange(new TextPosition(2, 0), new TextPosition(2, 6))));
        var symbol3 = new ProcedureSymbol("Proc1", new Location("file:///test.rtm", new TextRange(new TextPosition(3, 0), new TextPosition(3, 5))));

        index.AddDefinition(symbol1);
        index.AddDefinition(symbol2);
        index.AddDefinition(symbol3);

        // Act
        var allDefinitions = index.AllDefinitions.ToList();

        // Assert
        Assert.Equal(3, allDefinitions.Count);
        Assert.Contains(symbol1, allDefinitions);
        Assert.Contains(symbol2, allDefinitions);
        Assert.Contains(symbol3, allDefinitions);
    }

    [Fact]
    public void AllReferences_ReturnsAllAddedReferences()
    {
        // Arrange
        var index = new SymbolIndex();
        var ref1 = new FieldReference { Name = "Field1", Location = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 0), new TextPosition(1, 6))) };
        var ref2 = new FieldReference { Name = "Field2", Location = new Location("file:///test.rtm", new TextRange(new TextPosition(2, 0), new TextPosition(2, 6))) };

        index.AddReference(ref1);
        index.AddReference(ref2);

        // Act
        var allReferences = index.AllReferences.ToList();

        // Assert
        Assert.Equal(2, allReferences.Count);
        Assert.Contains(ref1, allReferences);
        Assert.Contains(ref2, allReferences);
    }
}
