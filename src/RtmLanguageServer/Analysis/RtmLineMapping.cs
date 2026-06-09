namespace RtmLanguageServer.Analysis;

public sealed record RtmLineMapping(
    int GeneratedLine,
    string OriginalUri,
    int OriginalLine,
    string? SourceName,
    string? SourceSectionName);