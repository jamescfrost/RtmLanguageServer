using RtmLanguageServer.Core.Common;
using RtmLanguageServer.Core.Parsing;
using RtmLanguageServer.Core.Symbols;

namespace RtmLanguageServer.Protocol;

public sealed class RtmLanguageService
{
    private readonly DocumentStore _documents;

    public RtmLanguageService(DocumentStore documents) => _documents = documents;

    public IReadOnlyList<Location> GetDefinition(TextDocumentPositionParams request)
    {
        var symbol = ResolveSymbol(request);
        return symbol is not null ? [symbol.Location] : [];
    }

    public IReadOnlyList<Location> FindReferences(ReferenceParams request)
    {
        var symbol = ResolveSymbol(request);
        if (symbol is null)
            return [];

        var refs = new List<Location>();
        foreach (var doc in _documents.All)
            refs.AddRange(doc.Index.FindReferences(symbol, request.Context.IncludeDeclaration));

        refs.Sort(LocationEqualityComparer.Instance);
        return refs.Distinct(LocationEqualityComparer.Instance).ToList();
    }

    public Hover? Hover(TextDocumentPositionParams request)
    {
        var symbol = ResolveSymbol(request);
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

    private RtmSymbol? ResolveSymbol(TextDocumentPositionParams request)
    {
        if (!_documents.TryGet(request.TextDocument.Uri, out var document) || document is null)
            return null;

        return document.ResolveSymbolAt(request.Position, _documents.All);
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

    private static string DisplayUri(string uri) => RtmUri.DisplayName(uri);
}
