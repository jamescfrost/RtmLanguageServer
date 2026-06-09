using Antlr4.Runtime;

namespace RtmLanguageServer.Core.Parsing;

public sealed class ThrowingLexerErrorListener : IAntlrErrorListener<int>
{
    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        int offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        throw new InvalidOperationException(
            $"Lexer error at {line}:{charPositionInLine}: {msg}");
    }
}