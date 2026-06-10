using RtmLanguageServer.Core.Analysis;
using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.Symbols;

public record ProcedureSymbol(
    string Name,
    Location Location)
    : RtmSymbol(Name, Location)
{
    public override string Label => "procedure";

    public List<FieldReference> Parameters { get; } = [];

    public override string GetMarkdown(SymbolIndex? index = null)
    {
        return $"`{Name}{GetParametersMarkdown()}`";
    }

    public string GetParametersMarkdown()
    {
        var paramStrings = Parameters.Select(p =>
        {
            var result = p.Name;

            if (p.ResolvedSymbol is FieldSymbol field)
            {
                if (!string.IsNullOrWhiteSpace(field.EditMask))
                    result += $" {field.EditMask}";

                if (field.Dimensions is { Length: > 0 })
                    result += $"*{string.Join("*", field.Dimensions)}";
            }

            return result;
        });

        return $"({string.Join(", ", paramStrings)})";
    }
}
