using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis;

public sealed record RtmReference(
    string Name,
    string Uri,
    TextRange Range
);