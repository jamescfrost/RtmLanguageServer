using Antlr4.Runtime;
using RtmLanguageServer.Core.Common;

namespace RtmLanguageServer.Core.Parsing;

/// <summary>
/// Collects syntax errors as structured diagnostic objects.
/// </summary>
/// <typeparam name="TSymbol">Type of offending symbol (int for lexer, IToken for parser)</typeparam>
public sealed class CollectingErrorListener<TSymbol> : IAntlrErrorListener<TSymbol>
{
    private readonly List<RtmDiagnostic> _diagnostics;
    private readonly string _source;

    public CollectingErrorListener(List<RtmDiagnostic> diagnostics, string source = "antlr")
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
        var position = new TextPosition(line - 1, charPositionInLine);
        var range = new TextRange(position, new TextPosition(line - 1, charPositionInLine + 1));

        _diagnostics.Add(new RtmDiagnostic(
            range,
            DiagnosticSeverity.Error,
            _source.ToLowerInvariant(),
            msg));
    }
}
