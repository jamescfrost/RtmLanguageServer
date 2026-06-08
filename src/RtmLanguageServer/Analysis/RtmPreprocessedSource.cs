using RtmLanguageServer.Analysis.Core;

namespace RtmLanguageServer.Analysis;

public sealed class RtmPreprocessedSource
{
    public RtmPreprocessedSource(
        string text,
        IReadOnlyDictionary<string, RtmNameSection> nameSections,
        IReadOnlyList<RtmLineMapping> lineMappings)
    {
        Text = text;
        NameSections = nameSections;
        LineMappings = lineMappings;
    }

    public string Text { get; }

    public IReadOnlyDictionary<string, RtmNameSection> NameSections { get; }

    public IReadOnlyList<RtmLineMapping> LineMappings { get; }

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
}