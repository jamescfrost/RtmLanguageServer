namespace RtmLanguageServer.Core.Parsing;

public sealed record RtmSourceLine(
    int OriginalLine,
    string Text);