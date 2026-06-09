using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using RtmLanguageServer.Core.Analysis;
using RtmLanguageServer.Core.Common;
using RtmLanguageServer.Core.Symbols;
using RtmLanguageServer.Core.Generated;

namespace RtmLanguageServer.Core.Parsing;

public sealed class RtmDocument
{
    private readonly CommonTokenStream _tokens;
    public RtmPreprocessedSource Preprocessed { get; }

    private RtmDocument(
        string uri,
        string text,
        CommonTokenStream tokens,
        SymbolIndex index,
        IReadOnlyList<string> diagnostics,
        RtmPreprocessedSource preprocessed)
    {
        Uri = uri;
        Text = text;
        _tokens = tokens;
        Index = index;
        Diagnostics = diagnostics;
        Preprocessed = preprocessed;
    }

    public string Uri { get; }
    public string Text { get; }
    public SymbolIndex Index { get; }
    public IReadOnlyList<string> Diagnostics { get; }

    public static RtmDocument Parse(string uri, string text, RtmWorkspaceIndex? workspace = null)
    {
        uri = RtmUri.Normalize(uri);
        var diagnostics = new List<string>();

        workspace ??= new RtmWorkspaceIndex();
        workspace.AddSource(uri, text);

        var preprocessed = RtmPreprocessor.Process(
            uri,
            text,
            workspace);

        var input = new AntlrInputStream(preprocessed.Text);
        var lexer = new RtmLexer(input);

        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new CollectingLexerErrorListener(diagnostics));

        var tokens = new CommonTokenStream(lexer);

        var parser = new RtmParser(tokens)
        {
            BuildParseTree = true
        };

        parser.RemoveErrorListeners();
        parser.AddErrorListener(new CollectingParserErrorListener(diagnostics));

        var tree = parser.start();
        tokens.Fill();

        var index = new SymbolIndex();

        foreach (var section in preprocessed.NameSections.Values)
        {
            var range = new TextRange(
                new TextPosition(section.HeaderLine - 1, section.HeaderColumn),
                new TextPosition(section.HeaderLine - 1, section.HeaderColumn + section.Name.Length));

            index.AddDefinition(new NameIncludeSymbol(
                section.Name,
                new Location(section.Uri, range)));
        }


        foreach (var include in preprocessed.IncludeDirectives)
        {
            var range = new TextRange(
                new TextPosition(include.Line - 1, include.Column),
                new TextPosition(include.Line - 1, include.Column + include.Length));

            index.AddReference(new IncludeReference
            {
                Name = include.IncludeName,
                IncludeName = include.IncludeName,
                SourceName = include.SourceName,
                Location = new Location(include.Uri, range)
            });
        }

        ParseTreeWalker.Default.Walk(
            new RtmAnalyzer(uri, index, preprocessed),
            tree);

        var document = new RtmDocument(
            uri,
            text,
            tokens,
            index,
            diagnostics,
            preprocessed);

        SemanticResolver.Resolve(document);

        return document;
    }

    public IToken? TokenAt(TextPosition position)
    {
        foreach (var token in _tokens.GetTokens())
        {
            if (token.Type == TokenConstants.EOF) continue;
            if (token.Channel != TokenConstants.DefaultChannel) continue;
            var range = token.GetRange();
            if (range.Contains(position)) return token;
        }

        return null;
    }

    public string? SymbolNameAt(TextPosition position)
    {
        var token = TokenAt(position);
        if (!string.IsNullOrWhiteSpace(token?.Text))
            return RtmAnalyzer.CleanName(token!.Text);

        var line = Text.Split('\n').ElementAtOrDefault(position.Line);
        if (line is null) return null;

        // VS Code gives positions as UTF-16 code units. For ordinary RTM source this
        // character index is good enough and keeps the implementation simple.
        var ch = Math.Clamp(position.Character, 0, line.Length);
        var start = ch;
        while (start > 0 && IsIdentifierChar(line[start - 1])) start--;
        var end = ch;
        while (end < line.Length && IsIdentifierChar(line[end])) end++;

        return end > start ? RtmAnalyzer.CleanName(line[start..end]) : null;
    }

    public string? WordAt(TextPosition position) => SymbolNameAt(position);

    public RtmSymbol? DefinitionAt(TextPosition position) => Index.AllDefinitions
        .FirstOrDefault(symbol =>
            string.Equals(symbol.Location.Uri, Uri, StringComparison.OrdinalIgnoreCase)
            && symbol.Location.Range.Contains(position));

    public RtmReference? ReferenceAt(TextPosition position) => Index.AllReferences
        .FirstOrDefault(reference =>
            string.Equals(reference.Location.Uri, Uri, StringComparison.OrdinalIgnoreCase)
            && reference.Location.Range.Contains(position));

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '.' or '$' or '@' or '-';

    private sealed class CollectingLexerErrorListener : IAntlrErrorListener<int>
    {
        private readonly List<string> _diagnostics;
        public CollectingLexerErrorListener(List<string> diagnostics) => _diagnostics = diagnostics;

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line,
            int charPositionInLine, string msg, RecognitionException e)
            => _diagnostics.Add($"Lexer {line}:{charPositionInLine}: {msg}");
    }

    private sealed class CollectingParserErrorListener : IAntlrErrorListener<IToken>
    {
        private readonly List<string> _diagnostics;
        public CollectingParserErrorListener(List<string> diagnostics) => _diagnostics = diagnostics;

        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line,
            int charPositionInLine, string msg, RecognitionException e)
            => _diagnostics.Add($"Parser {line}:{charPositionInLine}: {msg}");
    }

    public TextRange MapGeneratedRange(TextRange generatedRange)
    {
        return Preprocessed.MapRange(generatedRange);
    }

    public Location MapGeneratedLocation(Location generatedLocation)
    {
        return Preprocessed.MapLocation(generatedLocation.Uri, generatedLocation.Range);
    }
}
