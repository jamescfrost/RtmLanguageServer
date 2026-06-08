using RtmLanguageServer.Analysis.Symbols;

namespace RtmLanguageServer.Analysis.Core;

public sealed class SymbolIndex
{
    private readonly Dictionary<string, List<RtmSymbol>> _definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<Location>> _references = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RtmSymbol> _allDefinitions = [];
    private readonly List<RtmReference> _allReferences = [];

    public IReadOnlyDictionary<string, List<RtmSymbol>> Definitions => _definitions;
    public IReadOnlyDictionary<string, List<Location>> References => _references;

    public IEnumerable<RtmSymbol> AllDefinitions => _allDefinitions;
    public IEnumerable<RtmReference> AllReferences => _allReferences;

    public void AddDefinition(RtmSymbol symbol, bool indexByName = true)
    {
        if (string.IsNullOrWhiteSpace(symbol.Name)) return;

        if (!_allDefinitions.Any(s => SameSymbolIdentity(s, symbol)))
            _allDefinitions.Add(symbol);

        symbol.Scope?.Define(symbol);

        if (!indexByName)
            return;

        if (!_definitions.TryGetValue(symbol.Name, out var list))
        {
            list = [];
            _definitions[symbol.Name] = list;
        }

        if (!list.Any(s => SameSymbolIdentity(s, symbol)))
            list.Add(symbol);
    }

    public void AddReference(RtmReference reference)
    {
        if (string.IsNullOrWhiteSpace(reference.Name))
            return;

        if (!_references.TryGetValue(reference.Name, out var list))
        {
            list = [];
            _references[reference.Name] = list;
        }

        var location = reference.Location;

        if (!list.Any(l => l.SameSpan(location)))
            list.Add(location);

        if (!_allReferences.Any(r => SameReferenceSpan(r, location)))
            _allReferences.Add(reference);
    }

    public IReadOnlyList<RtmSymbol> FindDefinitions(string name) =>
        _definitions.TryGetValue(name, out var list) ? list : [];

    public IReadOnlyList<Location> FindReferences(RtmSymbol symbol, bool includeDeclaration)
    {
        var result = new List<Location>();

        result.AddRange(_allReferences
            .Where(r => r.Name == symbol.Name)
            .Select(r => r.Location));

        if (includeDeclaration)
            result.Add(symbol.Location);

        return result
            .GroupBy(LocationKey)
            .Select(g => g.First())
            .ToList();
    }

    public RtmSymbol? DefinitionAt(TextPosition position) =>
        AllDefinitions.FirstOrDefault(symbol => symbol.Location.Range.Contains(position));

    public RtmReference? ReferenceAt(TextPosition position) =>
        AllReferences.FirstOrDefault(reference => reference.Location.Range.Contains(position));

    private static bool SameSymbolIdentity(RtmSymbol left, RtmSymbol right) =>
        left.Location.Uri.Equals(right.Location.Uri, StringComparison.OrdinalIgnoreCase)
        && left.Location.Range == right.Location.Range
        && left.GetType() == right.GetType();

    private static bool SameReferenceSpan(RtmReference reference, Location location) =>
        string.Equals(reference.Location.Uri, location.Uri, StringComparison.OrdinalIgnoreCase)
        && reference.Location.Range == location.Range;

    private static string LocationKey(Location l) =>
        $"{l.Uri}:{l.Range.Start.Line}:{l.Range.Start.Character}:{l.Range.End.Line}:{l.Range.End.Character}";
}