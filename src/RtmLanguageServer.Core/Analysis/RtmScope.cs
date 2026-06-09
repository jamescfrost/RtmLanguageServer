using RtmLanguageServer.Core.Symbols;

namespace RtmLanguageServer.Core.Analysis;

public sealed class RtmScope
{
    private readonly Dictionary<string, List<RtmSymbol>> _symbols = new(StringComparer.OrdinalIgnoreCase);

    public RtmScope(RtmScope? parent = null, RtmSymbol? owner = null)
    {
        Parent = parent;
        Owner = owner;
    }

    public RtmScope? Parent { get; }
    public RtmSymbol? Owner { get; }

    public void Define(RtmSymbol symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol.Name)) return;
        if (!_symbols.TryGetValue(symbol.Name, out var list))
        {
            list = [];
            _symbols[symbol.Name] = list;
        }

        if (!list.Any(s => s.GetType() == symbol.GetType() && s.Location.Range == symbol.Location.Range))
            list.Add(symbol);
    }

    public RtmSymbol? Resolve(string name)
    {
        for (var scope = this; scope is not null; scope = scope.Parent)
        {
            if (scope._symbols.TryGetValue(name, out var list))
                return list.LastOrDefault();
        }

        return null;
    }
}
