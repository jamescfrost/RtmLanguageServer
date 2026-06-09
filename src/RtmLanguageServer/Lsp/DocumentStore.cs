using System.Collections.Concurrent;
using RtmLanguageServer.Analysis;

namespace RtmLanguageServer.Lsp;

public sealed class DocumentStore
{
    private readonly ConcurrentDictionary<string, RtmDocument> _documents = new(StringComparer.OrdinalIgnoreCase);
    private readonly RtmWorkspaceIndex _workspace = new();

    public RtmDocument OpenOrUpdate(string uri, string text)
    {
        _workspace.AddSource(uri, text);
        var document = RtmDocument.Parse(uri, text, _workspace);
        _documents[uri] = document;
        return document;
    }

    public bool TryGet(string uri, out RtmDocument? document) => _documents.TryGetValue(uri, out document);

    public IEnumerable<RtmDocument> All => _documents.Values;
}
