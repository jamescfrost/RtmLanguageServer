namespace RtmLanguageServer.Analysis;

public sealed record RtmNameSection(
    string Name,
    int HeaderLine,
    int HeaderColumn,
    IReadOnlyList<RtmSourceLine> Lines);