using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public sealed record FileSymbol(
    string Name,
    string Uri,
    TextRange Range,
    TextRange SelectionRange,
    string? Detail = null)
    : RtmSymbol(Name, Uri, Range, SelectionRange, Detail)
{
    public override string Label => "file";
    public override string GetMarkdown() => $"{Name} ";
}