namespace RtmLanguageServer.Core.Parsing;

public sealed record RtmSourceFile(
    string SourceName,
    string Uri,
    string Text,
    IReadOnlyDictionary<string, RtmNameSection> NameSections);
