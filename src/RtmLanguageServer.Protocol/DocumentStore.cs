using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RtmLanguageServer.Core.Analysis;
using RtmLanguageServer.Core.Common;
using RtmLanguageServer.Core.Parsing;

namespace RtmLanguageServer.Protocol;

public sealed class DocumentStore
{
    public event Action<WorkspaceStatus>? Status;

    private readonly ConcurrentDictionary<string, RtmDocument> _documents = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _texts = new(StringComparer.OrdinalIgnoreCase);
    private readonly RtmWorkspaceIndex _workspace = new();
    private readonly HashSet<string> _loadedRoots = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _explicitWorkspaceRoots = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private readonly ILogger<DocumentStore> _logger;

    public DocumentStore(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DocumentStore>();
    }

    public void LoadWorkspace(string? rootUri)
    {
        var rootPath = UriToLocalPath(rootUri);
        if (string.IsNullOrWhiteSpace(rootPath))
            return;

        if (File.Exists(rootPath))
            rootPath = Path.GetDirectoryName(rootPath);

        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            return;

        rootPath = Path.GetFullPath(rootPath);

        lock (_gate)
        {
            _explicitWorkspaceRoots.Add(rootPath);
            Report(WorkspaceStatus.Milestone($"RTM: loading workspace {Path.GetFileName(rootPath)}", isBusy: true));
            LoadWorkspaceFiles(rootPath);
            RebuildWorkspaceAndDocumentsUnsafe();
            Report(WorkspaceStatus.Milestone($"RTM: workspace symbols ready ({_documents.Count} files indexed)", isBusy: false));
        }
    }

    public RtmDocument OpenOrUpdate(string uri, string text)
    {
        uri = RtmUri.Normalize(uri);

        lock (_gate)
        {
            _texts[uri] = text;

            EnsureFallbackWorkspaceLoaded(uri);
            Report(WorkspaceStatus.Trace($"RTM: processing {DisplayFileName(uri)}: loading symbols"));
            RebuildWorkspaceAndDocumentsUnsafe();

            var document = ParseAndStoreUnsafe(uri, text);
            SemanticResolver.ResolveAll(_documents.Values);
            Report(WorkspaceStatus.Milestone($"RTM: ready ({_documents.Count} files indexed)", isBusy: false));
            return document;
        }
    }

    public bool TryGet(string uri, out RtmDocument? document)
    {
        uri = RtmUri.Normalize(uri);

        if (_documents.TryGetValue(uri, out document))
            return true;

        lock (_gate)
        {
            if (_documents.TryGetValue(uri, out document))
                return true;

            TryLoadUriFromDisk(uri, overwrite: false);
            RebuildWorkspaceAndDocumentsUnsafe();
            return _documents.TryGetValue(uri, out document);
        }
    }

    public IEnumerable<RtmDocument> All => _documents.Values.ToArray();

    private void EnsureFallbackWorkspaceLoaded(string uri)
    {
        var containingFolder = GetContainingFolder(uri);
        if (string.IsNullOrWhiteSpace(containingFolder) || !Directory.Exists(containingFolder))
            return;

        containingFolder = Path.GetFullPath(containingFolder);

        if (_explicitWorkspaceRoots.Count > 0)
        {
            foreach (var workspaceRoot in _explicitWorkspaceRoots)
            {
                if (IsSameOrChildPath(containingFolder, workspaceRoot))
                    LoadWorkspaceFiles(workspaceRoot);
            }

            return;
        }

        LoadWorkspaceFiles(containingFolder);
    }

    private static bool IsSameOrChildPath(string path, string root)
    {
        path = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return string.Equals(path, root, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private void LoadWorkspaceFiles(string rootPath)
    {
        rootPath = Path.GetFullPath(rootPath);
        if (!_loadedRoots.Add(rootPath))
            return;

        var count = 0;
        foreach (var path in EnumerateRtmFiles(rootPath))
        {
            Report(WorkspaceStatus.Trace($"RTM: processing file {Path.GetFileName(path)}: reading source"));
            TryLoadFile(path, overwrite: false);
            count++;
        }

        Report(WorkspaceStatus.Trace($"RTM: discovered {count} .rtm files under {Path.GetFileName(rootPath)}"));
    }

    private IEnumerable<string> EnumerateRtmFiles(string rootPath)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
            MatchCasing = MatchCasing.CaseInsensitive
        };

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(rootPath, "*.rtm", options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not enumerate RTM files in {Directory}", rootPath);
            return [];
        }

        return files.Where(filePath => !IsInIgnoredDirectory(filePath));
    }

    private static bool IsInIgnoredDirectory(string filePath)
    {
        var ignoredDirectories = new[] { ".git", ".vs", ".vscode", "bin", "obj", "node_modules" };

        var pathParts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return pathParts.Any(part => 
            ignoredDirectories.Contains(part, StringComparer.OrdinalIgnoreCase));
    }

    private void TryLoadFile(string path, bool overwrite)
    {
        try
        {
            var uri = RtmUri.Normalize(Path.GetFullPath(path));
            var text = File.ReadAllText(path);

            if (overwrite)
                _texts[uri] = text;
            else
                _texts.TryAdd(uri, text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read file {FilePath}", path);
        }
    }

    private void TryLoadUriFromDisk(string uri, bool overwrite)
    {
        try
        {
            var path = RtmUri.ToLocalPath(uri);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            var normalizedUri = RtmUri.Normalize(path);
            var text = File.ReadAllText(path);

            if (overwrite)
                _texts[normalizedUri] = text;
            else
                _texts.TryAdd(normalizedUri, text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load URI from disk {Uri}", uri);
        }
    }

    private void RebuildWorkspaceAndDocumentsUnsafe()
    {
        _workspace.Clear();

        foreach (var (uri, text) in _texts)
            _workspace.AddSource(uri, text);

        var rebuilt = new Dictionary<string, RtmDocument>(StringComparer.OrdinalIgnoreCase);

        foreach (var (uri, text) in _texts)
        {
            try
            {
                Report(WorkspaceStatus.Trace($"RTM: processing file {DisplayFileName(uri)}: loading symbols"));
                rebuilt[uri] = RtmDocument.Parse(uri, text, _workspace);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not parse document {Uri}", uri);
                Report(WorkspaceStatus.Important($"RTM: could not index {DisplayFileName(uri)}: {ex.Message}"));
                if (_documents.TryGetValue(uri, out var previous))
                    rebuilt[uri] = previous;
            }
        }

        _documents.Clear();
        foreach (var (uri, document) in rebuilt)
            _documents[uri] = document;

        SemanticResolver.ResolveAll(_documents.Values);
    }

    private RtmDocument ParseAndStoreUnsafe(string uri, string text)
    {
        _workspace.AddSource(uri, text);
        var document = RtmDocument.Parse(uri, text, _workspace);
        _documents[uri] = document;
        return document;
    }

    private void Report(WorkspaceStatus status) => Status?.Invoke(status);

    private static string DisplayFileName(string uri)
    {
        var path = RtmUri.ToLocalPath(uri);
        return string.IsNullOrWhiteSpace(path) ? uri : Path.GetFileName(path);
    }

    private static string? UriToLocalPath(string? uri) => RtmUri.ToLocalPath(uri);

    private static string? GetContainingFolder(string uri)
    {
        var localPath = RtmUri.ToLocalPath(uri);
        return string.IsNullOrWhiteSpace(localPath) ? null : Path.GetDirectoryName(localPath);
    }
}
