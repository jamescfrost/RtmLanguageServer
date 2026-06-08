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

    public static RtmPreprocessedSource Process(string source)
    {
        var originalLines = SplitLines(source);
        var sections = ExtractNameSections(originalLines);

        return BuildExpandedSource(originalLines, sections);
    }

    private static Dictionary<string, RtmNameSection> ExtractNameSections(IReadOnlyList<string> lines)
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
                currentHeaderLine,
                currentHeaderColumn,
                currentLines.ToArray());
        }
    }

    private static RtmPreprocessedSource BuildExpandedSource(
        IReadOnlyList<string> originalLines,
        IReadOnlyDictionary<string, RtmNameSection> sections)
    {
        var output = new StringBuilder();
        var mappings = new List<RtmLineMapping>();

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

                if (sourceName == "*" && sections.TryGetValue(includeName, out var section))
                {
                    foreach (var sectionLine in section.Lines)
                    {
                        AppendGeneratedLine(
                            sectionLine.Text,
                            sectionLine.OriginalLine,
                            section.Name);
                    }

                    continue;
                }
            }

            AppendGeneratedLine(line, originalLine, null);
        }

        return new RtmPreprocessedSource(
            output.ToString(),
            sections,
            mappings);

        void AppendGeneratedLine(string text, int originalLine, string? sourceSectionName)
        {
            output.AppendLine(text);

            mappings.Add(new RtmLineMapping(
                GeneratedLine: generatedLine,
                OriginalLine: originalLine,
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

    private static string[] SplitLines(string source)
    {
        return source
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');
    }
}