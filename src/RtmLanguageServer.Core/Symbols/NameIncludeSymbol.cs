using RtmLanguageServer.Core.Analysis;
using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.Symbols;

public sealed record NameIncludeSymbol : RtmSymbol
{
    public NameIncludeSymbol(
        string name,
        Location location)
        : base(name, location)
    {
    }

    public override string Label => "Name Include";

    public override string GetMarkdown(SymbolIndex? index = null) => $"`$NAME {Name}`";
}
