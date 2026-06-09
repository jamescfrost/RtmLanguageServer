using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.Symbols;

public sealed record FieldSymbol(
    string Name,
    Location Location)
    : RtmSymbol(Name, Location)
{
    public override string Label => "data field";

    public string? EditMask { get; set; }
    public int[]? Dimensions { get; set; }
    public string? AssignedFrom { get; set; }

    public override string GetMarkdown()
    {
        var parts = new List<string> { $"`{Name}`" };

        if (!string.IsNullOrWhiteSpace(EditMask))
            parts.Add($"mask `{EditMask}`");

        if (Dimensions is { Length: > 0 })
            parts.Add($"dimensions `{string.Join(" × ", Dimensions)}`");

        if (!string.IsNullOrWhiteSpace(AssignedFrom))
            parts.Add($"assigned from `{AssignedFrom}`");

        return string.Join("  ", parts);
    }
}