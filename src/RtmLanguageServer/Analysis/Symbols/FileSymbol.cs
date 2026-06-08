using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public sealed record FileSymbol(
    string Name,
    Location Location)
    : RtmSymbol(Name, Location)
{
    public override string Label => "file";
    public override string GetMarkdown() => $"`{Name}`";
}