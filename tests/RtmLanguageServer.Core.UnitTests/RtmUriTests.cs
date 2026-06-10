using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.UnitTests;

public class RtmUriTests
{
    [Fact]
    public void Normalize_WithFileUri_ReturnsNormalizedUri()
    {
        // Arrange
        var input = "file:///D:/test.rtm";

        // Act
        var result = RtmUri.Normalize(input);

        // Assert
        Assert.StartsWith("file:///", result);
        Assert.Contains("test.rtm", result.ToLowerInvariant());
    }

    [Fact]
    public void Normalize_WithWindowsPath_ConvertsToFileUri()
    {
        // Arrange
        var input = "D:\\test.rtm";

        // Act
        var result = RtmUri.Normalize(input);

        // Assert
        Assert.StartsWith("file:///", result);
        Assert.Contains("test.rtm", result.ToLowerInvariant());
    }

    [Fact]
    public void Normalize_WithUnixPath_ConvertsToFileUri()
    {
        // Arrange
        var input = "/home/user/test.rtm";

        // Act
        var result = RtmUri.Normalize(input);

        // Assert
        Assert.StartsWith("file:///", result);
    }

    [Fact]
    public void Normalize_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = RtmUri.Normalize("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Normalize_WithWhitespace_ReturnsWhitespace()
    {
        // Act
        var result = RtmUri.Normalize("   ");

        // Assert
        Assert.Equal("   ", result);
    }

    [Fact]
    public void Normalize_WithRelativePath_ReturnsAsIs()
    {
        // Arrange
        var input = "test.rtm";

        // Act
        var result = RtmUri.Normalize(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void ToLocalPath_WithFileUri_ReturnsLocalPath()
    {
        // Arrange
        var input = "file:///D:/test.rtm";

        // Act
        var result = RtmUri.ToLocalPath(input);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("test.rtm", result);
    }

    [Fact]
    public void ToLocalPath_WithWindowsPath_ReturnsPath()
    {
        // Arrange
        var input = "D:\\test.rtm";

        // Act
        var result = RtmUri.ToLocalPath(input);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("test.rtm", result);
    }

    [Fact]
    public void ToLocalPath_WithEmptyString_ReturnsNull()
    {
        // Act
        var result = RtmUri.ToLocalPath("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToLocalPath_WithNull_ReturnsNull()
    {
        // Act
        var result = RtmUri.ToLocalPath(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToLocalPath_WithRelativePath_ReturnsNull()
    {
        // Arrange
        var input = "test.rtm";

        // Act
        var result = RtmUri.ToLocalPath(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SourceNameFromUri_WithFileUri_ReturnsUppercaseNameWithoutExtension()
    {
        // Arrange
        var input = "file:///D:/path/to/myfile.rtm";

        // Act
        var result = RtmUri.SourceNameFromUri(input);

        // Assert
        Assert.Equal("MYFILE", result);
    }

    [Fact]
    public void SourceNameFromUri_WithLocalPath_ReturnsUppercaseNameWithoutExtension()
    {
        // Arrange
        var input = "D:\\path\\to\\myfile.rtm";

        // Act
        var result = RtmUri.SourceNameFromUri(input);

        // Assert
        Assert.Equal("MYFILE", result);
    }

    [Fact]
    public void SourceNameFromUri_ConvertsToUppercase()
    {
        // Arrange
        var input = "file:///test.rtm";

        // Act
        var result = RtmUri.SourceNameFromUri(input);

        // Assert
        Assert.Equal("TEST", result);
        Assert.True(result == result.ToUpperInvariant());
    }

    [Fact]
    public void DisplayName_WithFileUri_ReturnsFileName()
    {
        // Arrange
        var input = "file:///D:/path/to/myfile.rtm";

        // Act
        var result = RtmUri.DisplayName(input);

        // Assert
        Assert.Equal("myfile.rtm", result);
    }

    [Fact]
    public void DisplayName_WithLocalPath_ReturnsFileName()
    {
        // Arrange
        var input = "D:\\path\\to\\myfile.rtm";

        // Act
        var result = RtmUri.DisplayName(input);

        // Assert
        Assert.Equal("myfile.rtm", result);
    }

    [Fact]
    public void DisplayName_WithJustFileName_ReturnsFileName()
    {
        // Arrange
        var input = "myfile.rtm";

        // Act
        var result = RtmUri.DisplayName(input);

        // Assert
        Assert.Equal("myfile.rtm", result);
    }

    [Theory]
    [InlineData("file:///D:/test.rtm", "file:///D:/test.rtm")]
    [InlineData("D:\\test.rtm", "file:///D:/test.rtm")]
    [InlineData("file:///C:/Users/test.rtm", "file:///C:/Users/test.rtm")]
    public void Normalize_MultipleFormats_ProducesConsistentUris(string input, string expectedStart)
    {
        // Act
        var result = RtmUri.Normalize(input);

        // Assert
        Assert.StartsWith("file:///", result);
    }

    [Fact]
    public void Normalize_PreservesFileName()
    {
        // Arrange
        var filename = "MyTestFile.rtm";
        var input = $"D:\\path\\to\\{filename}";

        // Act
        var result = RtmUri.Normalize(input);

        // Assert
        Assert.Contains(filename, result);
    }
}
