using RtmLanguageServer.Core.Analysis;
using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.Symbols;

public abstract record RtmSymbol(
    string Name,
    Location Location)
{
    public abstract string Label { get; }

    public OverlaySymbol? Overlay { get; set; }

    public RtmScope? Scope { get; set; }

    public abstract string GetMarkdown(SymbolIndex? index = null);
}
