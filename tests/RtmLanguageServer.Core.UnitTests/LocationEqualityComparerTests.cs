using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.UnitTests;

public class LocationEqualityComparerTests
{
    private readonly LocationEqualityComparer _comparer = LocationEqualityComparer.Instance;

    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        // Act
        var instance1 = LocationEqualityComparer.Instance;
        var instance2 = LocationEqualityComparer.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Equals_SameLocation_ReturnsTrue()
    {
        // Arrange
        var location1 = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));
        var location2 = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));

        // Act
        var result = _comparer.Equals(location1, location2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_DifferentUri_ReturnsFalse()
    {
        // Arrange
        var location1 = new Location("file:///test1.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));
        var location2 = new Location("file:///test2.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));

        // Act
        var result = _comparer.Equals(location1, location2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_DifferentRange_ReturnsFalse()
    {
        // Arrange
        var location1 = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));
        var location2 = new Location("file:///test.rtm", new TextRange(new TextPosition(2, 5), new TextPosition(2, 10)));

        // Act
        var result = _comparer.Equals(location1, location2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_CaseInsensitiveUri_ReturnsTrue()
    {
        // Arrange
        var location1 = new Location("file:///TEST.RTM", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));
        var location2 = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));

        // Act
        var result = _comparer.Equals(location1, location2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_BothNull_ReturnsTrue()
    {
        // Act
        var result = _comparer.Equals(null, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_OneNull_ReturnsFalse()
    {
        // Arrange
        var location = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));

        // Act
        var result1 = _comparer.Equals(location, null);
        var result2 = _comparer.Equals(null, location);

        // Assert
        Assert.False(result1);
        Assert.False(result2);
    }

    [Fact]
    public void GetHashCode_SameLocation_ReturnsSameHash()
    {
        // Arrange
        var location1 = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));
        var location2 = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));

        // Act
        var hash1 = _comparer.GetHashCode(location1);
        var hash2 = _comparer.GetHashCode(location2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_CaseInsensitiveUri_ReturnsSameHash()
    {
        // Arrange
        var location1 = new Location("file:///TEST.RTM", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));
        var location2 = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));

        // Act
        var hash1 = _comparer.GetHashCode(location1);
        var hash2 = _comparer.GetHashCode(location2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Compare_SameLocation_ReturnsZero()
    {
        // Arrange
        var location1 = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));
        var location2 = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));

        // Act
        var result = _comparer.Compare(location1, location2);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Compare_DifferentUri_SortsAlphabetically()
    {
        // Arrange
        var locationA = new Location("file:///a.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));
        var locationB = new Location("file:///b.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));

        // Act
        var result = _comparer.Compare(locationA, locationB);

        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void Compare_SameUri_DifferentLine_SortsByLine()
    {
        // Arrange
        var location1 = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));
        var location2 = new Location("file:///test.rtm", new TextRange(new TextPosition(2, 5), new TextPosition(2, 10)));

        // Act
        var result = _comparer.Compare(location1, location2);

        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void Compare_SameUriAndLine_DifferentCharacter_SortsByCharacter()
    {
        // Arrange
        var location1 = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));
        var location2 = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 10), new TextPosition(1, 15)));

        // Act
        var result = _comparer.Compare(location1, location2);

        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void Compare_NullFirst_ReturnsNegative()
    {
        // Arrange
        var location = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));

        // Act
        var result = _comparer.Compare(null, location);

        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void Compare_NullSecond_ReturnsPositive()
    {
        // Arrange
        var location = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));

        // Act
        var result = _comparer.Compare(location, null);

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void Compare_BothNull_ReturnsZero()
    {
        // Act
        var result = _comparer.Compare(null, null);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CanBeUsedInHashSet()
    {
        // Arrange
        var location1 = new Location("file:///test.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));
        var location2 = new Location("file:///TEST.RTM", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10))); // Same but different case
        var location3 = new Location("file:///test.rtm", new TextRange(new TextPosition(2, 5), new TextPosition(2, 10))); // Different line

        var set = new HashSet<Location>(_comparer);

        // Act
        set.Add(location1);
        set.Add(location2); // Should not add (same as location1)
        set.Add(location3); // Should add (different)

        // Assert
        Assert.Equal(2, set.Count);
        Assert.Contains(location1, set);
        Assert.Contains(location3, set);
    }

    [Fact]
    public void CanBeUsedInListSort()
    {
        // Arrange
        var location1 = new Location("file:///c.rtm", new TextRange(new TextPosition(3, 10), new TextPosition(3, 15)));
        var location2 = new Location("file:///a.rtm", new TextRange(new TextPosition(1, 5), new TextPosition(1, 10)));
        var location3 = new Location("file:///b.rtm", new TextRange(new TextPosition(2, 7), new TextPosition(2, 12)));

        var list = new List<Location> { location1, location2, location3 };

        // Act
        list.Sort(_comparer);

        // Assert
        Assert.Equal(location2, list[0]); // a.rtm
        Assert.Equal(location3, list[1]); // b.rtm
        Assert.Equal(location1, list[2]); // c.rtm
    }
}
