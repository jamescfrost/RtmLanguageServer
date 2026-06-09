using Antlr4.Runtime;

namespace RtmLanguageServer.Core.Parsing;

public sealed class ThrowingParserErrorListener : BaseErrorListener
{
    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        throw new Exception($"Parse error at {line}:{charPositionInLine}: {msg}");
    }
}