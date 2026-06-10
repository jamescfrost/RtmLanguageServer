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

    /// <summary>
    /// Adds symbol-specific lines to the hover display.
    /// Base implementation adds overlay information if applicable.
    /// </summary>
    public virtual void AddHoverDetails(List<string> lines, SymbolIndex? index)
    {
        // Default: show overlay for non-overlay symbols
        if (this is not OverlaySymbol && Overlay is not null)
        {
            lines.Add($"Overlay: `{Overlay.Name}`");
        }
    }
}
