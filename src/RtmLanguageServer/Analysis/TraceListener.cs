using Antlr4.Runtime;
using RtmLanguageServer.Generated;

namespace RtmLanguageServer.Analysis;

public sealed class TraceListener : RtmParserBaseListener
{
    public override void EnterEveryRule(ParserRuleContext context)
    {
        Console.Error.WriteLine(
            $"{context.GetType().Name} -> {context.GetText()}");
    }
}