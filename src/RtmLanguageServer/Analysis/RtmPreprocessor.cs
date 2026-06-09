using System.Text;
using System.Text.RegularExpressions;

namespace RtmLanguageServer.Analysis;

public static class RtmPreprocessor
{
    private static readonly Regex NameHeaderRegex = new(
        @"^\s*\$NAME\s+(?<name>\.{3,4}|[A-Za-z@][A-Za-z0-9@._-]*)\s*(?:<<.*)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex IncludeRegex = new(
        @"^\s*\$INCLUDE\s+(?<source>\*|[^\s(]+)\((?<name>[^)]+)\)\s*(?:<<.*)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static RtmPreprocessedSource Process(
        string uri,
        string source,
        RtmWorkspaceIndex workspace)
    {
        var currentSourceName = GetSourceNameFromUri(uri);
        var originalLines = SplitLines(source);

        return BuildExpandedSource(
            uri,
            currentSourceName,
            originalLines,
            workspace);
    }

    public static Dictionary<string, RtmNameSection> ExtractNameSections(string uri, string source)
    {
        return ExtractNameSections(uri, SplitLines(source));
    }

    public static Dictionary<string, RtmNameSection> ExtractNameSections(string uri, IReadOnlyList<string> lines)
    {
        var result = new Dictionary<string, RtmNameSection>(StringComparer.OrdinalIgnoreCase);

        string? currentName = null;
        int currentHeaderLine = 0;
        int currentHeaderColumn = 0;
        var currentLines = new List<RtmSourceLine>();

        for (var i = 0; i < lines.Count; i++)
        {
            var originalLine = i + 1;
            var line = lines[i];

            var nameMatch = NameHeaderRegex.Match(line);

            if (nameMatch.Success)
            {
                SaveCurrent();

                currentName = nameMatch.Groups["name"].Value.Trim();
                currentHeaderLine = originalLine;
                currentHeaderColumn = nameMatch.Groups["name"].Index;
                currentLines.Clear();
                continue;
            }

            if (IsNameTerminator(line))
            {
                SaveCurrent();
                currentName = null;
                currentLines.Clear();
            }

            if (currentName is not null)
            {
                currentLines.Add(new RtmSourceLine(originalLine, line));
            }
        }

        SaveCurrent();

        return result;

        void SaveCurrent()
        {
            if (string.IsNullOrWhiteSpace(currentName))
                return;

            result[currentName] = new RtmNameSection(
                currentName,
                uri,
                currentHeaderLine,
                currentHeaderColumn,
                currentLines.ToArray());
        }
    }

    private static RtmPreprocessedSource BuildExpandedSource(
        string currentUri, 
        string currentSourceName, 
        IReadOnlyList<string> originalLines,
        RtmWorkspaceIndex workspace)
    {
        var sections = ExtractNameSections(currentUri, originalLines);
        var output = new StringBuilder();
        var mappings = new List<RtmLineMapping>();
        var includeDirectives = new List<RtmIncludeDirective>();

        var generatedLine = 1;
        var insideNameSection = false;

        for (var i = 0; i < originalLines.Count; i++)
        {
            var originalLine = i + 1;
            var line = originalLines[i];

            if (NameHeaderRegex.IsMatch(line))
            {
                insideNameSection = true;
                continue;
            }

            if (insideNameSection)
            {
                if (!IsNameTerminator(line))
                    continue;

                insideNameSection = false;
            }

            var include = IncludeRegex.Match(line);

            if (include.Success)
            {
                var sourceName = include.Groups["source"].Value.Trim();
                var includeName = include.Groups["name"].Value.Trim();
                includeDirectives.Add(new RtmIncludeDirective(
                    sourceName,
                    includeName,
                    currentUri,
                    originalLine,
                    include.Groups["name"].Index,
                    include.Groups["name"].Length));

                var section = workspace.FindSection(
                    currentSourceName,
                    sourceName,
                    includeName);

                if (section is not null)
                {
                    foreach (var sectionLine in section.Lines)
                    {
                        AppendGeneratedLine(
                            sectionLine.Text,
                            section.Uri,
                            sectionLine.OriginalLine,
                            sourceName == "*" ? currentSourceName : sourceName,
                            section.Name);
                    }

                    continue;
                }
            }

            AppendGeneratedLine(line, currentUri, originalLine, currentSourceName, null);
        }

        return new RtmPreprocessedSource(
            output.ToString(),
            sections,
            mappings,
            includeDirectives);

        void AppendGeneratedLine(
            string text,
            string originalUri,
            int originalLine,
            string? sourceName,
            string? sourceSectionName)
        {
            output.AppendLine(text);

            mappings.Add(new RtmLineMapping(
                GeneratedLine: generatedLine,
                OriginalUri: originalUri,
                OriginalLine: originalLine,
                SourceName: sourceName,
                SourceSectionName: sourceSectionName));

            generatedLine++;
        }
    }

    private static bool IsNameTerminator(string line)
    {
        var trimmed = line.TrimStart();

        return trimmed.StartsWith("$NAME", StringComparison.OrdinalIgnoreCase)
               || trimmed.StartsWith("$ENTRY", StringComparison.OrdinalIgnoreCase)
               || trimmed.StartsWith("$END", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSourceNameFromUri(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.IsFile)
            return Path.GetFileNameWithoutExtension(parsed.LocalPath).ToUpperInvariant();

        return Path.GetFileNameWithoutExtension(uri).ToUpperInvariant();
    }

    private static string[] SplitLines(string source)
    {
        return source
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');
    }
}