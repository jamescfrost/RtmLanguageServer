using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public abstract class RtmReference
{
    public string Name { get; init; } = "";
    public Location Location { get; init; } = default!;
    public RtmSymbol? ResolvedSymbol { get; set; }
    public OverlaySymbol? Overlay { get; set; }
}