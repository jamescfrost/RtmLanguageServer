using RtmLanguageServer.Core.Analysis;
using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.Symbols;

public sealed record ExternalSymbol(
    string Name,
    Location Location)
    : RtmSymbol(Name, Location)
{
    public override string Label => "external";
    public override string GetMarkdown(SymbolIndex? index = null) => $"`{Name}`";
}
