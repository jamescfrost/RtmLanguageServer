namespace RtmLanguageServer.Analysis.Symbols;

public sealed class IncludeReference : RtmReference
{
    public string SourceName { get; init; } = "";
    public string IncludeName { get; init; } = "";
}