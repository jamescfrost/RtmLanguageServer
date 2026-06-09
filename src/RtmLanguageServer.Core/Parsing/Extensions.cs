using RtmLanguageServer.Core.Common;
using RtmLanguageServer.Core.Symbols;

namespace RtmLanguageServer.Core.Parsing;

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
            return RedirectExternalToWorkspaceOverlay(exactDefinition, allDocuments, document.Uri)
                ?? exactDefinition;

        var exactReference = allDocuments
            .SelectMany(d => d.Index.AllReferences)
            .Where(r => IsInDocumentAt(r.Location, document.Uri, position))
            .OrderBy(r => SpanLength(r.Location.Range))
            .FirstOrDefault();

        if (exactReference is OverlayReference overlayReference)
        {
            var overlayDefinition = FindWorkspaceOverlayDefinition(overlayReference.Name, allDocuments, document.Uri);
            if (overlayDefinition is not null)
                return overlayDefinition;
        }

        if (exactReference?.ResolvedSymbol is not null)
            return RedirectExternalToWorkspaceOverlay(exactReference.ResolvedSymbol, allDocuments, document.Uri)
                ?? exactReference.ResolvedSymbol;

        var name = exactReference?.Name ?? document.ResolveSymbolName(position);
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var sourceName = RtmUri.SourceNameFromUri(document.Uri);

        return allDocuments
            .SelectMany(d => d.Index.FindDefinitions(name))
            .OrderByDescending(s => SymbolTypeScore(exactReference, s))
            .ThenByDescending(s => s is OverlaySymbol)
            .ThenBy(s => s is ExternalSymbol)
            .ThenByDescending(s => string.Equals(s.Location.Uri, document.Uri, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(s => string.Equals(RtmUri.SourceNameFromUri(s.Location.Uri), sourceName, StringComparison.OrdinalIgnoreCase))
            .ThenBy(s => s.Location.Range.Start.Line)
            .ThenBy(s => s.Location.Range.Start.Character)
            .FirstOrDefault();
    }

    private static OverlaySymbol? RedirectExternalToWorkspaceOverlay(
        RtmSymbol symbol,
        IEnumerable<RtmDocument> documents,
        string currentUri)
    {
        if (symbol is not ExternalSymbol)
            return null;

        return FindWorkspaceOverlayDefinition(symbol.Name, documents, currentUri);
    }

    private static OverlaySymbol? FindWorkspaceOverlayDefinition(
        string name,
        IEnumerable<RtmDocument> documents,
        string currentUri)
    {
        return documents
            .SelectMany(d => d.Index.FindDefinitions(name))
            .OfType<OverlaySymbol>()
            .OrderByDescending(s => string.Equals(s.Location.Uri, currentUri, StringComparison.OrdinalIgnoreCase))
            .ThenBy(s => s.Location.Range.Start.Line)
            .ThenBy(s => s.Location.Range.Start.Character)
            .FirstOrDefault();
    }

    private static bool IsInDocumentAt(Location location, string documentUri, TextPosition position) =>
        string.Equals(location.Uri, documentUri, StringComparison.OrdinalIgnoreCase)
        && location.Range.Contains(position);

    private static int SpanLength(TextRange range) =>
        ((range.End.Line - range.Start.Line) * 100_000) + (range.End.Character - range.Start.Character);

    private static int SymbolTypeScore(RtmReference? reference, RtmSymbol symbol) => reference switch
    {
        OverlayReference when symbol is OverlaySymbol => 100,
        OverlayReference when symbol is ExternalSymbol => -100,
        ProcedureReference when symbol is ProcedureSymbol => 100,
        ProcedureReference when symbol is ExternalSymbol => 10,
        FieldReference when symbol is FieldSymbol => 100,
        IncludeReference when symbol is NameIncludeSymbol => 100,
        _ => 0
    };
}
