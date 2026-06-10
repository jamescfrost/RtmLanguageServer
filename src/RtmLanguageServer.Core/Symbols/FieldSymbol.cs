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
        builder.AppendLine($"`{Name}`");
        var combinedEditMask = new StringBuilder();
        var editMask = string.IsNullOrWhiteSpace(EditMask) ? "?" : EditMask;
        combinedEditMask.Append(editMask);
        foreach(var dimension in Dimensions ?? Array.Empty<int>())
            combinedEditMask.Append($"*{dimension}");

        builder.AppendLine($" `{combinedEditMask}`");
        if (!string.IsNullOrWhiteSpace(AssignedFrom))
        {
            builder.AppendLine();
            builder.AppendLine($"assigned from `{AssignedFrom}`");
        }

        return builder.ToString();
    }
}
