using RtmLanguageServer.Analysis;
using RtmLanguageServer.Analysis.Core;
using RtmLanguageServer.Analysis.Symbols;

namespace RtmLanguageServer.Lsp;

public sealed class RtmLanguageService
{
    private readonly DocumentStore _documents;

    public RtmLanguageService(DocumentStore documents) => _documents = documents;

    public IReadOnlyList<Location> GetDefinition(TextDocumentPositionParams request)
    {
        if (!_documents.TryGet(request.TextDocument.Uri, out var document) || document is null)
            return [];

        var symbol = document.ResolveSymbolAt(request.Position, _documents.All);
        if (symbol is null)
            return [];

        return [symbol.Location];
    }

    public IReadOnlyList<Location> FindReferences(ReferenceParams request)
    {
        if (!_documents.TryGet(request.TextDocument.Uri, out var document) || document is null)
            return [];

        var symbol = document.ResolveSymbolAt(request.Position, _documents.All);
        if (symbol is null)
            return [];

        var refs = new List<Location>();
        foreach (var doc in _documents.All)
            refs.AddRange(doc.Index.FindReferences(symbol, request.Context.IncludeDeclaration));

        return refs
            .DistinctBy(LocationKey)
            .OrderBy(l => l.Uri)
            .ThenBy(l => l.Range.Start.Line)
            .ThenBy(l => l.Range.Start.Character)
            .ToList();
    }

    public Hover? Hover(TextDocumentPositionParams request)
    {
        if (!_documents.TryGet(request.TextDocument.Uri, out var document) || document is null)
            return null;

        var symbol = document.ResolveSymbolAt(request.Position, _documents.All);
        if (symbol is null)
            return null;

        return new Hover
        {
            Contents = new MarkupContent
            {
                Kind = "markdown",
                Value = BuildHoverMarkdown(symbol)
            },
            Range = symbol.Location.Range
        };
    }

    private static string BuildHoverMarkdown(RtmSymbol symbol)
    {
        var lines = new List<string>
        {
            $"**{ToTitle(symbol.Label)}** `{symbol.Name}`"
        };

        var detail = symbol.GetMarkdown();
        if (!string.IsNullOrWhiteSpace(detail))
            lines.Add(detail);

        if (symbol is not OverlaySymbol && symbol.Overlay is not null)
            lines.Add($"Overlay: `{symbol.Overlay.Name}`");

        if (symbol is OverlaySymbol overlayWithChildren && overlayWithChildren.Children.Count > 0)
        {
            var childSummary = overlayWithChildren.Children
                .GroupBy(c => c.Label)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Count()} {g.Key}");

            lines.Add($"Children: {string.Join(", ", childSummary)}");
        }

        lines.Add($"Defined in `{DisplayUri(symbol.Location.Uri)}`");

        return string.Join("\n\n", lines);
    }

    private static string ToTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Symbol";

        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    private static string LocationKey(Location l) =>
        $"{l.Uri}:{l.Range.Start.Line}:{l.Range.Start.Character}:{l.Range.End.Line}:{l.Range.End.Character}";

    private static string DisplayUri(string uri) => RtmUri.DisplayName(uri);
}