using RtmLanguageServer.Analysis.Symbols;

namespace RtmLanguageServer.Analysis.Core;

public sealed class SymbolIndex
{
    private readonly Dictionary<string, List<RtmSymbol>> _definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<Location>> _references = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, List<RtmSymbol>> Definitions => _definitions;
    public IReadOnlyDictionary<string, List<Location>> References => _references;

    public IEnumerable<RtmSymbol> AllDefinitions => _definitions.Values.SelectMany(v => v);

    public IEnumerable<RtmReference> AllReferences =>
        _references.SelectMany(kvp =>
            kvp.Value.Select(location => new RtmReference(kvp.Key, location.Uri, location.Range)));

    public void AddDefinition(RtmSymbol symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol.Name)) return;
        if (!_definitions.TryGetValue(symbol.Name, out var list))
        {
            list = [];
            _definitions[symbol.Name] = list;
        }

        if (!list.Any(s =>
                s.Uri.Equals(symbol.Uri, StringComparison.OrdinalIgnoreCase)
                && s.SelectionRange == symbol.SelectionRange
                && s.GetType() == symbol.GetType()))
        {
            list.Add(symbol);
        }
    }

    public void AddReference(string name, Location location)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        if (!_references.TryGetValue(name, out var list))
        {
            list = [];
            _references[name] = list;
        }

        if (!list.Any(l => SourceRange.SameSpan(l, location)))
            list.Add(location);
    }

    public IReadOnlyList<RtmSymbol> FindDefinitions(string name) =>
        _definitions.TryGetValue(name, out var list) ? list : [];

    public IReadOnlyList<Location> FindReferences(string name, bool includeDeclaration)
    {
        var result = new List<Location>();
        if (_references.TryGetValue(name, out var refs)) result.AddRange(refs);
        if (includeDeclaration && _definitions.TryGetValue(name, out var defs))
            result.AddRange(defs.Select(d => new Location(d.Uri, d.SelectionRange)));

        return result
            .GroupBy(l =>
                $"{l.Uri}:{l.Range.Start.Line}:{l.Range.Start.Character}:{l.Range.End.Line}:{l.Range.End.Character}")
            .Select(g => g.First())
            .OrderBy(l => l.Uri)
            .ThenBy(l => l.Range.Start.Line)
            .ThenBy(l => l.Range.Start.Character)
            .ToList();
    }

    public RtmSymbol? ResolveBestDefinition(string name) => FindDefinitions(name).FirstOrDefault();

    public RtmSymbol? DefinitionAt(TextPosition position) =>
        AllDefinitions.FirstOrDefault(symbol => SourceRange.Contains(symbol.SelectionRange, position));

    public RtmReference? ReferenceAt(TextPosition position) =>
        AllReferences.FirstOrDefault(reference => SourceRange.Contains(reference.Range, position));
}