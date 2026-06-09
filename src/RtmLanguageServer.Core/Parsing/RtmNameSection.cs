namespace RtmLanguageServer.Core.Parsing;

public sealed record RtmNameSection(
    string Name,
    string Uri,
    int HeaderLine,
    int HeaderColumn,
    IReadOnlyList<RtmSourceLine> Lines);
