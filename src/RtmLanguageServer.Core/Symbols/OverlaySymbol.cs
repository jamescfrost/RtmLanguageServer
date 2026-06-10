using RtmLanguageServer.Core.Analysis;
using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.Symbols;

public sealed record OverlaySymbol(
    string Name,
    Location Location)
    : RtmSymbol(Name, Location)
{
    public override string Label => "overlay";

    public override string GetMarkdown(SymbolIndex? index = null)
    {
        if (index is null)
            return $"`{Name}(<Indeterminate Parameters>)`";

        // Find the child program to get the signature
        var program = index.AllDefinitions
            .OfType<ProgramSymbol>()
            .FirstOrDefault(p => p.Overlay?.Name == Name);

        return program is not null
            ? $"`{Name}{program.GetParametersMarkdown()}`"
            : $"`{Name}()`";
    }

    public override void AddHoverDetails(List<string> lines, SymbolIndex? index)
    {
        // Overlays don't show parent overlay info (base does nothing for OverlaySymbol)
        // but they show children summary
        if (index is null)
            return;

        var children = index.AllDefinitions
            .Where(s => s.Overlay?.Name == Name && s is not OverlaySymbol)
            .ToList();

        if (children.Count > 0)
        {
            var childSummary = children
                .GroupBy(c => c.Label)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Count()} {g.Key}");

            lines.Add($"Children: {string.Join(", ", childSummary)}");
        }
    }
}
