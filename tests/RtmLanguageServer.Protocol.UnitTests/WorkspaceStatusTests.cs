namespace RtmLanguageServer.Protocol.UnitTests;

public class WorkspaceStatusTests
{
    [Fact]
    public void Trace_WithMessage_ReturnsWorkspaceStatusWithTraceKindAndNotBusy()
    {
        // Arrange
        var message = "Test trace message";

        // Act
        var result = WorkspaceStatus.Trace(message);

        // Assert
        Assert.Equal(message, result.Message);
        Assert.Equal(StatusKind.Trace, result.Kind);
        Assert.False(result.IsBusy);
    }

    [Fact]
    public void Trace_WithEmptyString_ReturnsWorkspaceStatusWithEmptyMessage()
    {
        // Arrange
        var message = string.Empty;

        // Act
        var result = WorkspaceStatus.Trace(message);

        // Assert
        Assert.Equal(message, result.Message);
        Assert.Equal(StatusKind.Trace, result.Kind);
        Assert.False(result.IsBusy);
    }

    [Fact]
    public void Milestone_WithMessage_ReturnsWorkspaceStatusWithMilestoneKindAndNotBusy()
    {
        // Arrange
        var message = "Test milestone message";

        // Act
        var result = WorkspaceStatus.Milestone(message);

        // Assert
        Assert.Equal(message, result.Message);
        Assert.Equal(StatusKind.Milestone, result.Kind);
        Assert.False(result.IsBusy);
    }

    [Fact]
    public void Milestone_WithMessageAndIsBusyTrue_ReturnsWorkspaceStatusWithMilestoneKindAndBusy()
    {
        // Arrange
        var message = "Test milestone message";

        // Act
        var result = WorkspaceStatus.Milestone(message, isBusy: true);

        // Assert
        Assert.Equal(message, result.Message);
        Assert.Equal(StatusKind.Milestone, result.Kind);
        Assert.True(result.IsBusy);
    }

    [Fact]
    public void Milestone_WithMessageAndIsBusyFalse_ReturnsWorkspaceStatusWithMilestoneKindAndNotBusy()
    {
        // Arrange
        var message = "Test milestone message";

        // Act
        var result = WorkspaceStatus.Milestone(message, isBusy: false);

        // Assert
        Assert.Equal(message, result.Message);
        Assert.Equal(StatusKind.Milestone, result.Kind);
        Assert.False(result.IsBusy);
    }

    [Fact]
    public void Important_WithMessage_ReturnsWorkspaceStatusWithImportantKindAndNotBusy()
    {
        // Arrange
        var message = "Test important message";

        // Act
        var result = WorkspaceStatus.Important(message);

        // Assert
        Assert.Equal(message, result.Message);
        Assert.Equal(StatusKind.Important, result.Kind);
        Assert.False(result.IsBusy);
    }

    [Fact]
    public void Important_WithEmptyString_ReturnsWorkspaceStatusWithEmptyMessage()
    {
        // Arrange
        var message = string.Empty;

        // Act
        var result = WorkspaceStatus.Important(message);

        // Assert
        Assert.Equal(message, result.Message);
        Assert.Equal(StatusKind.Important, result.Kind);
        Assert.False(result.IsBusy);
    }
}
