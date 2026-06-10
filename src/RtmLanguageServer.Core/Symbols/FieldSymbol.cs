using System.Text;
using RtmLanguageServer.Core.Analysis;
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

    public override string GetMarkdown(SymbolIndex? index = null)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{Name} ");
        var editMask = string.IsNullOrWhiteSpace(EditMask) ? "?" : EditMask;
        builder.Append(editMask);
        foreach(var dimension in Dimensions ?? Array.Empty<int>())
            builder.Append($"*{dimension}");

        return $"`{builder.ToString()}`";
    }

    public override void AddHoverDetails(List<string> lines, SymbolIndex? index)
    {
        if (!string.IsNullOrWhiteSpace(AssignedFrom))
        {
            lines.Add($"assigned from `{AssignedFrom}`");
        }
    }
}
