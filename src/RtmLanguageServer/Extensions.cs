using RtmLanguageServer.Analysis;
using RtmLanguageServer.Analysis.Core;
using RtmLanguageServer.Analysis.Symbols;

namespace RtmLanguageServer;

public static class Extensions
{
    public static string? ResolveSymbolName(this RtmDocument document, TextPosition position)
    {
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

        var localReference = document.ReferenceAt(position);
        if (localReference?.ResolvedSymbol is not null)
            return localReference.ResolvedSymbol;

        var name = localReference is null
            ? document.ResolveSymbolName(position)
            : localReference.Name;

        if (string.IsNullOrWhiteSpace(name))
            return null;

        return documents
            .SelectMany(d => d.Index.FindDefinitions(name))
            .OrderByDescending(s => string.Equals(s.Location.Uri, document.Uri, StringComparison.OrdinalIgnoreCase))
            .ThenBy(s => s.Location.Range.Start.Line)
            .ThenBy(s => s.Location.Range.Start.Character)
            .FirstOrDefault();
    }
}