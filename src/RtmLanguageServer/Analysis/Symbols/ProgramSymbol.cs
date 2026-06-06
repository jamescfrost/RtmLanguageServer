using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis.Symbols;

public sealed record ProgramSymbol(
    string Name,
    string Uri,
    TextRange Range,
    TextRange SelectionRange)
    : ProcedureSymbol(Name, Uri, Range, SelectionRange)
{
    public override string Label => "program";

    public List<RtmSymbol> Children { get; } = [];
}