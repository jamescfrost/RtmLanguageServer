using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.Symbols;

public record ProcedureSymbol(
    string Name,
    Location Location)
    : RtmSymbol(Name, Location)
{
    public override string Label => "procedure";

    public List<FieldReference> Parameters { get; } = [];

    public override string GetMarkdown() => $"`{Name}{GetParametersMarkdown()}`";

    public string GetParametersMarkdown() =>
        $"({string.Join(", ", Parameters.Select(p => p.Name))})";
}