using System.Collections.Concurrent;
using RtmLanguageServer.Analysis;

namespace RtmLanguageServer.Lsp;

public sealed class DocumentStore
{
    private readonly ConcurrentDictionary<string, RtmDocument> _documents = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _texts = new(StringComparer.OrdinalIgnoreCase);
    private readonly RtmWorkspaceIndex _workspace = new();
    private readonly object _gate = new();
    private bool _workspaceLoaded;

    public void LoadWorkspace(string? rootUri)
    {
        var rootPath = UriToLocalPath(rootUri);
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            return;

        lock (_gate)
        {
            LoadWorkspaceFiles(rootPath);
            _workspaceLoaded = true;
            RebuildWorkspaceAndDocumentsUnsafe();
        }
    }

    public RtmDocument OpenOrUpdate(string uri, string text)
    {
        uri = NormalizeUri(uri);

        lock (_gate)
        {
            _texts[uri] = text;

            // Some clients do not send a usable rootUri/workspaceFolders during initialize.
            // In that case, use the folder of the first opened RTM file as the workspace root
            // so cross-file overlays and $NAME includes are still preloaded.
            if (!_workspaceLoaded)
            {
                var containingFolder = GetContainingFolder(uri);
                if (!string.IsNullOrWhiteSpace(containingFolder) && Directory.Exists(containingFolder))
                {
                    LoadWorkspaceFiles(containingFolder);
                    _texts[uri] = text; // keep the editor buffer newer than the disk copy
                    _workspaceLoaded = true;
                }
            }

            RebuildWorkspaceAndDocumentsUnsafe();

            if (_documents.TryGetValue(uri, out var document))
                return document;

            _workspace.AddSource(uri, text);
            document = RtmDocument.Parse(uri, text, _workspace);
            _documents[uri] = document;
            SemanticResolver.ResolveAll(_documents.Values);
            return document;
        }
    }

    public bool TryGet(string uri, out RtmDocument? document) =>
        _documents.TryGetValue(NormalizeUri(uri), out document);

    public IEnumerable<RtmDocument> All => _documents.Values;

    private void LoadWorkspaceFiles(string rootPath)
    {
        foreach (var path in Directory.EnumerateFiles(rootPath, "*.rtm", SearchOption.AllDirectories))
            TryLoadFile(path);

        foreach (var path in Directory.EnumerateFiles(rootPath, "*.RTM", SearchOption.AllDirectories))
            TryLoadFile(path);
    }

    private void TryLoadFile(string path)
    {
        try
        {
            var uri = NormalizeUri(new Uri(Path.GetFullPath(path)).AbsoluteUri);
            _texts.TryAdd(uri, File.ReadAllText(path));
        }
        catch
        {
            // Ignore files that cannot be read; opened documents can still be indexed.
        }
    }

    private void RebuildWorkspaceAndDocumentsUnsafe()
    {
        _workspace.Clear();

        foreach (var (uri, text) in _texts)
            _workspace.AddSource(uri, text);

        foreach (var (uri, text) in _texts)
        {
            try
            {
                _documents[uri] = RtmDocument.Parse(uri, text, _workspace);
            }
            catch
            {
                // Keep the previous parsed document when a file is temporarily invalid.
            }
        }

        SemanticResolver.ResolveAll(_documents.Values);
    }

    private static string? UriToLocalPath(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return null;

        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.IsFile)
            return parsed.LocalPath;

        return Directory.Exists(uri) ? uri : null;
    }

    private static string? GetContainingFolder(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || !parsed.IsFile)
            return null;

        return Path.GetDirectoryName(parsed.LocalPath);
    }

    private static string NormalizeUri(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.IsFile)
            return new Uri(Path.GetFullPath(parsed.LocalPath)).AbsoluteUri;

        return uri;
    }
}
