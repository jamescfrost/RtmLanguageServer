using System.Collections.Concurrent;
using RtmLanguageServer.Analysis;

namespace RtmLanguageServer.Lsp;

public sealed class DocumentStore
{
    private readonly ConcurrentDictionary<string, RtmDocument> _documents = new(StringComparer.OrdinalIgnoreCase);

    public RtmDocument OpenOrUpdate(string uri, string text)
    {
        var document = RtmDocument.Parse(uri, text);
        _documents[uri] = document;
        return document;
    }

    public bool TryGet(string uri, out RtmDocument? document) => _documents.TryGetValue(uri, out document);

    public IEnumerable<RtmDocument> All => _documents.Values;
}
