using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public sealed record FieldSymbol(
    string Name,
    string Uri,
    TextRange Range,
    TextRange SelectionRange,
    string? Detail = null)
    : RtmSymbol(Name, Uri, Range, SelectionRange, Detail)
{
    public override string Label => "data field";

    public override string GetMarkdown()
    {
        var dimensionsText = $"[{string.Join(" * ", Dimensions ?? [])}]";
        return $"{Name} {EditMask} {dimensionsText}";
    }

    public string? EditMask { get; set; }
    public int[]? Dimensions { get; set; }
    public string? AssignedFrom { get; set; }
}