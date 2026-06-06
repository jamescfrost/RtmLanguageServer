using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public abstract record RtmSymbol(
    string Name,
    string Uri,
    TextRange Range,
    TextRange SelectionRange,
    string? Detail = null)
{
    public abstract string Label { get; }

    public OverlaySymbol? Overlay { get; set; }

    public abstract string GetMarkdown();
}