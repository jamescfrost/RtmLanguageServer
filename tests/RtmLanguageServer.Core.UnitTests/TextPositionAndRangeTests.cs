using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.UnitTests;

public class TextPositionTests
{
    [Fact]
    public void Constructor_SetsLineAndCharacter()
    {
        // Act
        var position = new TextPosition(5, 10);

        // Assert
        Assert.Equal(5, position.Line);
        Assert.Equal(10, position.Character);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var pos1 = new TextPosition(5, 10);
        var pos2 = new TextPosition(5, 10);

        // Act & Assert
        Assert.Equal(pos1, pos2);
        Assert.True(pos1.Equals(pos2));
    }

    [Fact]
    public void Equals_DifferentLine_ReturnsFalse()
    {
        // Arrange
        var pos1 = new TextPosition(5, 10);
        var pos2 = new TextPosition(6, 10);

        // Act & Assert
        Assert.NotEqual(pos1, pos2);
    }

    [Fact]
    public void Equals_DifferentCharacter_ReturnsFalse()
    {
        // Arrange
        var pos1 = new TextPosition(5, 10);
        var pos2 = new TextPosition(5, 11);

        // Act & Assert
        Assert.NotEqual(pos1, pos2);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        // Arrange
        var pos1 = new TextPosition(5, 10);
        var pos2 = new TextPosition(5, 10);

        // Act
        var hash1 = pos1.GetHashCode();
        var hash2 = pos2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ToString_ReturnsReadableFormat()
    {
        // Arrange
        var position = new TextPosition(5, 10);

        // Act
        var result = position.ToString();

        // Assert
        Assert.Contains("5", result);
        Assert.Contains("10", result);
    }

    [Fact]
    public void CanBeUsedInDictionary()
    {
        // Arrange
        var dict = new Dictionary<TextPosition, string>();
        var pos1 = new TextPosition(1, 5);
        var pos2 = new TextPosition(1, 5); // Same values
        var pos3 = new TextPosition(2, 5); // Different

        // Act
        dict[pos1] = "First";
        dict[pos2] = "Second"; // Should overwrite
        dict[pos3] = "Third";

        // Assert
        Assert.Equal(2, dict.Count);
        Assert.Equal("Second", dict[pos1]);
        Assert.Equal("Third", dict[pos3]);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 50)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void Constructor_AcceptsVariousValues(int line, int character)
    {
        // Act
        var position = new TextPosition(line, character);

        // Assert
        Assert.Equal(line, position.Line);
        Assert.Equal(character, position.Character);
    }
}

public class TextRangeTests
{
    [Fact]
    public void Constructor_SetsStartAndEnd()
    {
        // Arrange
        var start = new TextPosition(1, 5);
        var end = new TextPosition(1, 10);

        // Act
        var range = new TextRange(start, end);

        // Assert
        Assert.Equal(start, range.Start);
        Assert.Equal(end, range.End);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var range1 = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var range2 = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));

        // Act & Assert
        Assert.Equal(range1, range2);
    }

    [Fact]
    public void Equals_DifferentStart_ReturnsFalse()
    {
        // Arrange
        var range1 = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var range2 = new TextRange(new TextPosition(1, 6), new TextPosition(1, 10));

        // Act & Assert
        Assert.NotEqual(range1, range2);
    }

    [Fact]
    public void Equals_DifferentEnd_ReturnsFalse()
    {
        // Arrange
        var range1 = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var range2 = new TextRange(new TextPosition(1, 5), new TextPosition(1, 11));

        // Act & Assert
        Assert.NotEqual(range1, range2);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        // Arrange
        var range1 = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var range2 = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));

        // Act
        var hash1 = range1.GetHashCode();
        var hash2 = range2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Contains_PositionInside_ReturnsTrue()
    {
        // Arrange
        var range = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var position = new TextPosition(1, 7);

        // Act
        var result = range.Contains(position);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Contains_PositionAtStart_ReturnsTrue()
    {
        // Arrange
        var range = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var position = new TextPosition(1, 5);

        // Act
        var result = range.Contains(position);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Contains_PositionAtEnd_ReturnsFalse()
    {
        // Arrange - LSP ranges are end-exclusive
        var range = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var position = new TextPosition(1, 10); // Exactly at end

        // Act
        var result = range.Contains(position);

        // Assert
        Assert.False(result); // End-exclusive semantics
    }

    [Fact]
    public void Contains_PositionBefore_ReturnsFalse()
    {
        // Arrange
        var range = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var position = new TextPosition(1, 3);

        // Act
        var result = range.Contains(position);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Contains_PositionAfter_ReturnsFalse()
    {
        // Arrange
        var range = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var position = new TextPosition(1, 12);

        // Act
        var result = range.Contains(position);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Contains_PositionOnDifferentLine_ReturnsFalse()
    {
        // Arrange
        var range = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var position = new TextPosition(2, 7);

        // Act
        var result = range.Contains(position);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Contains_MultiLineRange_PositionInMiddle_ReturnsTrue()
    {
        // Arrange
        var range = new TextRange(new TextPosition(1, 5), new TextPosition(3, 10));
        var position = new TextPosition(2, 0);

        // Act
        var result = range.Contains(position);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ToString_ReturnsReadableFormat()
    {
        // Arrange
        var range = new TextRange(new TextPosition(1, 5), new TextPosition(2, 10));

        // Act
        var result = range.ToString();

        // Assert
        Assert.Contains("1", result);
        Assert.Contains("5", result);
        Assert.Contains("2", result);
        Assert.Contains("10", result);
    }

    [Fact]
    public void CanBeUsedInHashSet()
    {
        // Arrange
        var set = new HashSet<TextRange>();
        var range1 = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10));
        var range2 = new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)); // Same
        var range3 = new TextRange(new TextPosition(2, 5), new TextPosition(2, 10)); // Different

        // Act
        set.Add(range1);
        set.Add(range2); // Should not add
        set.Add(range3);

        // Assert
        Assert.Equal(2, set.Count);
    }
}
