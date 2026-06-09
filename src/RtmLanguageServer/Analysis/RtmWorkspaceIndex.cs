namespace RtmLanguageServer.Analysis;

public sealed class RtmWorkspaceIndex
{
    private readonly Dictionary<string, RtmSourceFile> _sources =
        new(StringComparer.OrdinalIgnoreCase);

    public void AddSource(string uri, string text)
    {
        var sourceName = GetSourceNameFromUri(uri);

        var sections = RtmPreprocessor.ExtractNameSections(uri, text);

        _sources[sourceName] = new RtmSourceFile(
            sourceName,
            uri,
            text,
            sections);
    }

    private static string GetSourceNameFromUri(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.IsFile)
            return Path.GetFileNameWithoutExtension(parsed.LocalPath).ToUpperInvariant();

        return Path.GetFileNameWithoutExtension(uri).ToUpperInvariant();
    }

    public RtmNameSection? FindSection(
        string currentSourceName,
        string sourceName,
        string includeName)
    {
        var actualSourceName = sourceName == "*"
            ? currentSourceName
            : sourceName;

        return _sources.TryGetValue(actualSourceName, out var source)
            && source.NameSections.TryGetValue(includeName, out var section)
                ? section
                : null;
    }
}