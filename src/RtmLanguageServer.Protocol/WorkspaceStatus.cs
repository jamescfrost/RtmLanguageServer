namespace RtmLanguageServer.Protocol;

public enum StatusKind
{
    /// <summary>General information, logged but not shown to user</summary>
    Trace,

    /// <summary>Important milestone, shown to user</summary>
    Milestone,

    /// <summary>Error or warning that should be visible to user</summary>
    Important
}

public sealed record WorkspaceStatus(
    string Message,
    StatusKind Kind = StatusKind.Trace,
    bool IsBusy = false)
{
    public static WorkspaceStatus Trace(string message) 
        => new(message, StatusKind.Trace, IsBusy: false);

    public static WorkspaceStatus Milestone(string message, bool isBusy = false) 
        => new(message, StatusKind.Milestone, isBusy);

    public static WorkspaceStatus Important(string message) 
        => new(message, StatusKind.Important, IsBusy: false);
}
