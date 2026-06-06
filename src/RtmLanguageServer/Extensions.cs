using RtmLanguageServer.Analysis;
using RtmLanguageServer.Analysis.Core;
using RtmLanguageServer.Analysis.Symbols;

namespace RtmLanguageServer;

public static class Extensions
{
    public static string? ResolveSymbolName(this RtmDocument document, TextPosition position)
    {
        // Prefer the indexed occurrence under the cursor. This means definition,
        // reference and hover work from either a declaration or a usage.
        var definition = document.DefinitionAt(position);
        if (definition is not null) return definition.Name;

        var reference = document.ReferenceAt(position);
        if (reference is not null) return reference.Name;

        return document.SymbolNameAt(position);
    }


    public static RtmSymbol? ResolveSymbolAt(this RtmDocument document,
        TextPosition position, IEnumerable<RtmDocument> documents)
    {
        var localDefinition = document.DefinitionAt(position);
        if (localDefinition is not null)
            return localDefinition;

        var name = document.ResolveSymbolName(position);
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return documents
            .SelectMany(d => d.Index.FindDefinitions(name))
            .OrderByDescending(s => string.Equals(s.Uri, document.Uri, StringComparison.OrdinalIgnoreCase))
            .ThenBy(s => s.SelectionRange.Start.Line)
            .ThenBy(s => s.SelectionRange.Start.Character)
            .FirstOrDefault();
    }
}