using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using RtmLanguageServer.Analysis.Core;
using RtmLanguageServer.Analysis.Symbols;
using RtmLanguageServer.Generated;

namespace RtmLanguageServer.Analysis;

public sealed class RtmDocument
{
    private readonly CommonTokenStream _tokens;

    private RtmDocument(string uri, string text, CommonTokenStream tokens, SymbolIndex index,
        IReadOnlyList<string> diagnostics)
    {
        Uri = uri;
        Text = text;
        _tokens = tokens;
        Index = index;
        Diagnostics = diagnostics;
    }

    public string Uri { get; }
    public string Text { get; }
    public SymbolIndex Index { get; }
    public IReadOnlyList<string> Diagnostics { get; }

    public static RtmDocument Parse(string uri, string text)
    {
        var diagnostics = new List<string>();
        var input = new AntlrInputStream(text);
        var lexer = new RtmLexer(input);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new CollectingLexerErrorListener(diagnostics));
        lexer.AddErrorListener(new ThrowingLexerErrorListener());

        var tokens = new CommonTokenStream(lexer);
        var parser = new RtmParser(tokens)
        {
            BuildParseTree = true
        };
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new CollectingParserErrorListener(diagnostics));

        parser.AddErrorListener(new ThrowingParserErrorListener());


        var tree = parser.start();
        tokens.Fill();

        ParseTreeWalker.Default.Walk(new TraceListener(), tree);
        var index = new SymbolIndex();
        ParseTreeWalker.Default.Walk(new RtmAnalyzer(uri, index), tree);

        return new RtmDocument(uri, text, tokens, index, diagnostics);
    }

    public IToken? TokenAt(TextPosition position)
    {
        foreach (var token in _tokens.GetTokens())
        {
            if (token.Type == TokenConstants.EOF) continue;
            if (token.Channel != TokenConstants.DefaultChannel) continue;
            var range = SourceRange.FromToken(token);
            if (SourceRange.Contains(range, position)) return token;
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

    public RtmSymbol? DefinitionAt(TextPosition position) => Index.DefinitionAt(position);

    public RtmReference? ReferenceAt(TextPosition position) => Index.ReferenceAt(position);

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
}