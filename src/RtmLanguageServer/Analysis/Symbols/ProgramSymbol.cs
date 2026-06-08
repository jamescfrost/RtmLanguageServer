using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public sealed record ProgramSymbol(
    string Name,
    Location Location)
    : ProcedureSymbol(Name, Location)
{
    public override string Label => "program";

    public List<RtmSymbol> Children { get; } = [];
}