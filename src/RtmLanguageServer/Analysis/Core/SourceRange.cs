using Antlr4.Runtime;

namespace RtmLanguageServer.Analysis.Core;

public static class SourceRange
{
    public static TextRange FromToken(IToken token)
    {
        var text = token.Text ?? string.Empty;
        var start = new TextPosition(Math.Max(token.Line - 1, 0), Math.Max(token.Column, 0));
        var end = new TextPosition(start.Line, start.Character + text.Length);
        return new TextRange(start, end);
    }

    public static bool Contains(TextRange range, TextPosition position)
    {
        if (position.Line < range.Start.Line || position.Line > range.End.Line) return false;
        if (position.Line == range.Start.Line && position.Character < range.Start.Character) return false;

        // LSP ranges are end-exclusive. This prevents hovering immediately after a token
        // from still selecting that token.
        if (position.Line == range.End.Line && position.Character >= range.End.Character) return false;
        return true;
    }

    public static bool SameSpan(Location left, Location right) =>
        string.Equals(left.Uri, right.Uri, StringComparison.OrdinalIgnoreCase)
        && left.Range.Start.Line == right.Range.Start.Line
        && left.Range.Start.Character == right.Range.Start.Character
        && left.Range.End.Line == right.Range.End.Line
        && left.Range.End.Character == right.Range.End.Character;
}