using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.Symbols;

public sealed record ProgramSymbol(
    string Name,
    Location Location)
    : ProcedureSymbol(Name, Location)
{
    public override string Label => "program";

    public List<RtmSymbol> Children { get; } = [];
}