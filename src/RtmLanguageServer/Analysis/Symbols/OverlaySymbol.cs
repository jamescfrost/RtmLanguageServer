using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public sealed record OverlaySymbol(
    string Name,
    string Uri,
    TextRange Range,
    TextRange SelectionRange,
    string? Detail = null)
    : RtmSymbol(Name, Uri, Range, SelectionRange, Detail)
{
    public override string Label => "overlay";

    public override string GetMarkdown() => $"{Name}{GetParametersMarkdown()}";

    public List<RtmSymbol> Children { get; } = [];

    private string GetParametersMarkdown()
    {
        var prog = Children.OfType<ProgramSymbol>().FirstOrDefault(p => p.Name == "$PROG");
        return prog?.GetParametersMarkdown() ?? "()";
    }
}