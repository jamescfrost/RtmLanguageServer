using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public sealed record NameIncludeSymbol : RtmSymbol
{
    public List<RtmSymbol> Children { get; } = [];

    public NameIncludeSymbol(
        string name,
        Location location)
        : base(name, location)
    {
    }

    public override string Label => "Name Include";

    public override string GetMarkdown() => $"`$NAME {Name}`";
}