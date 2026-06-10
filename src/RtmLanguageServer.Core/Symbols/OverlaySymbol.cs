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
}
