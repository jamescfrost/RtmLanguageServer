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
        var allDocuments = documents as IReadOnlyCollection<RtmDocument> ?? documents.ToArray();

        // First use exact source-location matches. This is important for symbols that were
        // parsed through an include expansion in a different document but whose mapped
        // location belongs to the file currently being hovered.
        var exactDefinition = allDocuments
            .SelectMany(d => d.Index.AllDefinitions)
            .Where(s => IsInDocumentAt(s.Location, document.Uri, position))
            .OrderBy(s => SpanLength(s.Location.Range))
            .ThenByDescending(s => string.Equals(s.Location.Uri, document.Uri, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        if (exactDefinition is not null)
            return exactDefinition;

        var exactReference = allDocuments
            .SelectMany(d => d.Index.AllReferences)
            .Where(r => IsInDocumentAt(r.Location, document.Uri, position))
            .OrderBy(r => SpanLength(r.Location.Range))
            .FirstOrDefault();

        if (exactReference?.ResolvedSymbol is not null)
            return exactReference.ResolvedSymbol;

        var name = exactReference?.Name ?? document.ResolveSymbolName(position);
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var sourceName = GetSourceNameFromUri(document.Uri);

        return allDocuments
            .SelectMany(d => d.Index.FindDefinitions(name))
            .OrderByDescending(s => string.Equals(s.Location.Uri, document.Uri, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(s => string.Equals(GetSourceNameFromUri(s.Location.Uri), sourceName, StringComparison.OrdinalIgnoreCase))
            .ThenBy(s => s.Location.Range.Start.Line)
            .ThenBy(s => s.Location.Range.Start.Character)
            .FirstOrDefault();
    }

    private static bool IsInDocumentAt(Location location, string documentUri, TextPosition position) =>
        string.Equals(location.Uri, documentUri, StringComparison.OrdinalIgnoreCase)
        && location.Range.Contains(position);

    private static int SpanLength(TextRange range) =>
        ((range.End.Line - range.Start.Line) * 100_000) + (range.End.Character - range.Start.Character);

    private static string GetSourceNameFromUri(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.IsFile)
            return Path.GetFileNameWithoutExtension(parsed.LocalPath).ToUpperInvariant();

        return Path.GetFileNameWithoutExtension(uri).ToUpperInvariant();
    }
}
