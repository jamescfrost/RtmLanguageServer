namespace RtmLanguageServer.Analysis;

public sealed record RtmSourceFile(
    string SourceName,
    string Uri,
    string Text,
    IReadOnlyDictionary<string, RtmNameSection> NameSections);
