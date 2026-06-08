using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public abstract record RtmSymbol(
    string Name,
    Location Location)
{
    public abstract string Label { get; }

    public OverlaySymbol? Overlay { get; set; }

    public RtmScope? Scope { get; set; }

    public abstract string GetMarkdown();
}