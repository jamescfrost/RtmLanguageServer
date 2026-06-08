namespace RtmLanguageServer.Analysis;

public sealed record RtmLineMapping(
    int GeneratedLine,
    int OriginalLine,
    string? SourceSectionName);