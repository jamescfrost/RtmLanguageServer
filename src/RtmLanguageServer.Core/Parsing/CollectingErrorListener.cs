using Antlr4.Runtime;

namespace RtmLanguageServer.Core.Parsing;

/// <summary>
/// Collects syntax errors into a list of diagnostic messages.
/// </summary>
/// <typeparam name="TSymbol">Type of offending symbol (int for lexer, IToken for parser)</typeparam>
public sealed class CollectingErrorListener<TSymbol> : IAntlrErrorListener<TSymbol>
{
    private readonly List<string> _diagnostics;
    private readonly string _source;

    public CollectingErrorListener(List<string> diagnostics, string source = "antlr")
    {
        _diagnostics = diagnostics;
        _source = source;
    }

    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        TSymbol offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        _diagnostics.Add($"{_source} {line}:{charPositionInLine}: {msg}");
    }
}
