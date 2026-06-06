using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public record ProcedureSymbol(
    string Name,
    string Uri,
    TextRange Range,
    TextRange SelectionRange,
    string? Detail = null)
    : RtmSymbol(Name, Uri, Range, SelectionRange, Detail)
{
    public override string Label => "procedure";

    public override string GetMarkdown() => $"{Name}({string.Join(", ", Parameters ?? [])})";

    public IReadOnlyList<string>? Parameters { get; set; }
}