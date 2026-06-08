namespace RtmLanguageServer.Analysis;

public sealed record RtmSourceLine(
    int OriginalLine,
    string Text);