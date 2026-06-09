using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.Parsing;

public sealed class RtmPreprocessedSource
{
    public RtmPreprocessedSource(
        string text,
        IReadOnlyDictionary<string, RtmNameSection> nameSections,
        IReadOnlyList<RtmLineMapping> lineMappings,
        IReadOnlyList<RtmIncludeDirective>? includeDirectives = null)
    {
        Text = text;
        NameSections = nameSections;
        LineMappings = lineMappings;
        IncludeDirectives = includeDirectives ?? [];
    }

    public string Text { get; }

    public IReadOnlyDictionary<string, RtmNameSection> NameSections { get; }

    public IReadOnlyList<RtmLineMapping> LineMappings { get; }

    public IReadOnlyList<RtmIncludeDirective> IncludeDirectives { get; }

    public TextRange MapRange(TextRange generatedRange)
    {
        return new TextRange(
            MapPosition(generatedRange.Start),
            MapPosition(generatedRange.End));
    }

    public TextPosition MapPosition(TextPosition generatedPosition)
    {
        var generatedLine = generatedPosition.Line + 1;

        var mapping = LineMappings.FirstOrDefault(m => m.GeneratedLine == generatedLine);

        if (mapping is null)
            return generatedPosition;

        return new TextPosition(
            mapping.OriginalLine - 1,
            generatedPosition.Character);
    }

    public Location MapLocation(string fallbackUri, TextRange generatedRange)
    {
        var startLine = generatedRange.Start.Line + 1;

        var mapping = LineMappings
            .FirstOrDefault(m => m.GeneratedLine == startLine);

        if (mapping is null)
            return new Location(fallbackUri, generatedRange);

        var mappedRange = new TextRange(
            new TextPosition(mapping.OriginalLine - 1, generatedRange.Start.Character),
            new TextPosition(mapping.OriginalLine - 1, generatedRange.End.Character));

        return new Location(mapping.OriginalUri, mappedRange);
    }
}