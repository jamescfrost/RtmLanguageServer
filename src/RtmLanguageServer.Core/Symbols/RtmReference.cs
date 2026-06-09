using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.Symbols;

public abstract class RtmReference
{
    public string Name { get; init; } = "";
    public Location Location { get; init; } = default!;
    public RtmSymbol? ResolvedSymbol { get; set; }
    public OverlaySymbol? Overlay { get; set; }
}