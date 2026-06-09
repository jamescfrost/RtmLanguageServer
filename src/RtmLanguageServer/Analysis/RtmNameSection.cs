namespace RtmLanguageServer.Analysis;

public sealed record RtmNameSection(
    string Name,
    string Uri,
    int HeaderLine,
    int HeaderColumn,
    IReadOnlyList<RtmSourceLine> Lines);
