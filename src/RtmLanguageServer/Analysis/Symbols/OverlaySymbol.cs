using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public sealed record OverlaySymbol(
    string Name,
    string Uri,
    TextRange Range,
    TextRange SelectionRange,
    string? Detail = null)
    : RtmSymbol(Name, Uri, Range, SelectionRange, Detail)
{
    public override string Label => "overlay";
    public override string GetMarkdown() => $"**Overlay:** {Name}";

    public List<RtmSymbol> Children { get; } = [];
}