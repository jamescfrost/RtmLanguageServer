using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Lsp;

public sealed class RtmLanguageService
{
    private readonly DocumentStore _documents;

    public RtmLanguageService(DocumentStore documents) => _documents = documents;

    public IReadOnlyList<Location> GetDefinition(TextDocumentPositionParams request)
    {
        if (!_documents.TryGet(request.TextDocument.Uri, out var document) || document is null) return [];

        var name = document.ResolveSymbolName(request.Position);
        if (string.IsNullOrWhiteSpace(name)) return [];

        return _documents.All
            .SelectMany(d => d.Index.FindDefinitions(name))
            .Select(s => new Location(s.Uri, s.SelectionRange))
            .DistinctBy(LocationKey)
            .OrderBy(l => l.Uri)
            .ThenBy(l => l.Range.Start.Line)
            .ThenBy(l => l.Range.Start.Character)
            .ToList();
    }

    public IReadOnlyList<Location> FindReferences(ReferenceParams request)
    {
        if (!_documents.TryGet(request.TextDocument.Uri, out var document) || document is null) return [];

        var name = document.ResolveSymbolName(request.Position);
        if (string.IsNullOrWhiteSpace(name)) return [];

        var refs = new List<Location>();
        foreach (var doc in _documents.All)
            refs.AddRange(doc.Index.FindReferences(name, request.Context.IncludeDeclaration));

        return refs
            .DistinctBy(LocationKey)
            .OrderBy(l => l.Uri)
            .ThenBy(l => l.Range.Start.Line)
            .ThenBy(l => l.Range.Start.Character)
            .ToList();
    }

    public Hover? Hover(TextDocumentPositionParams request)
    {
        if (!_documents.TryGet(request.TextDocument.Uri, out var document) || document is null) return null;

        var name = document.ResolveSymbolName(request.Position);
        if (string.IsNullOrWhiteSpace(name)) return null;

        var definition = _documents.All
            .SelectMany(d => d.Index.FindDefinitions(name))
            .OrderByDescending(s => string.Equals(s.Uri, document.Uri, StringComparison.OrdinalIgnoreCase))
            .ThenBy(s => s.SelectionRange.Start.Line)
            .ThenBy(s => s.SelectionRange.Start.Character)
            .FirstOrDefault();

        if (definition is null) return null;

        return new Hover
        {
            Contents = new MarkupContent
            {
                Kind = "markdown",
                Value = $"**{definition.GetMarkdown()}"
            },
            Range = definition.SelectionRange
        };
    }

    private static string LocationKey(Location l) =>
        $"{l.Uri}:{l.Range.Start.Line}:{l.Range.Start.Character}:{l.Range.End.Line}:{l.Range.End.Character}";

    private static string DisplayUri(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.IsFile)
            return Path.GetFileName(parsed.LocalPath);
        return uri;
    }
}