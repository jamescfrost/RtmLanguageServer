using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public sealed record OverlaySymbol(
    string Name,
    Location Location)
    : RtmSymbol(Name, Location)
{
    public override string Label => "overlay";

    public override string GetMarkdown() => $"`{Name}{GetParametersMarkdown()}`";

    public List<RtmSymbol> Children { get; } = [];

    public IEnumerable<FileSymbol> Files => Children.OfType<FileSymbol>();
    public IEnumerable<ExternalSymbol> Externals => Children.OfType<ExternalSymbol>();
    public IEnumerable<FieldSymbol> Fields => Children.OfType<FieldSymbol>();

    public IEnumerable<ProcedureSymbol> Procedures =>
        Children.OfType<ProcedureSymbol>().Where(p => p is not ProgramSymbol);

    public ProgramSymbol? Program => Children.OfType<ProgramSymbol>().FirstOrDefault();
    public IEnumerable<IncludeReference> Includes => Children.OfType<IncludeReference>();

    private string GetParametersMarkdown() => Program?.GetParametersMarkdown() ?? "()";
}