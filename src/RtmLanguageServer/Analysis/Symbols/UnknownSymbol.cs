using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public sealed record UnknownSymbol(
    string Name,
    Location Location)
    : RtmSymbol(Name, Location)
{
    public override string Label => "symbol";
    public override string GetMarkdown() => $"**Symbol:** {Name}";
}