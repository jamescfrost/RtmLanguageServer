using Antlr4.Runtime;
using RtmLanguageServer.Core.Generated;

namespace RtmLanguageServer.Core.Parsing;

public sealed class TraceListener : RtmParserBaseListener
{
    public override void EnterEveryRule(ParserRuleContext context)
    {
        Console.Error.WriteLine(
            $"{context.GetType().Name} -> {context.GetText()}");
    }
}