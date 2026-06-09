namespace RtmLanguageServer.Core.Parsing;

public sealed record RtmIncludeDirective(
    string SourceName,
    string IncludeName,
    string Uri,
    int Line,
    int Column,
    int Length);
