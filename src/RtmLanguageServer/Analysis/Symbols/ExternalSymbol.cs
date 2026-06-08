using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public sealed record ExternalSymbol(
    string Name,
    Location Location)
    : RtmSymbol(Name, Location)
{
    public override string Label => "external";
    public override string GetMarkdown() => $"**External:** {Name}";
}