using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public sealed record ExternalSymbol(
    string Name,
    string Uri,
    TextRange Range,
    TextRange SelectionRange,
    string? Detail = null)
    : RtmSymbol(Name, Uri, Range, SelectionRange, Detail)
{
    public override string Label => "external";
    public override string GetMarkdown() => $"**External:** {Name}";
}