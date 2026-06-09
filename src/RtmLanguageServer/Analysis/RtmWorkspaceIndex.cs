namespace RtmLanguageServer.Analysis;

public sealed class RtmWorkspaceIndex
{
    private readonly Dictionary<string, RtmSourceFile> _sources =
        new(StringComparer.OrdinalIgnoreCase);

    public void Clear() => _sources.Clear();

    public IEnumerable<RtmSourceFile> Sources => _sources.Values;

    public void AddSource(string uri, string text)
    {
        uri = RtmUri.Normalize(uri);
        var sourceName = RtmUri.SourceNameFromUri(uri);

        var sections = RtmPreprocessor.ExtractNameSections(uri, text);

        _sources[sourceName] = new RtmSourceFile(
            sourceName,
            uri,
            text,
            sections);
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