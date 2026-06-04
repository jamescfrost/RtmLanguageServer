using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using RtmLanguageServer.Analysis.Core;
using RtmLanguageServer.Analysis.Symbols;
using RtmLanguageServer.Generated;

namespace RtmLanguageServer.Analysis;

public sealed class RtmAnalyzer : RtmParserBaseListener
{
    private readonly string _uri;
    private readonly SymbolIndex _index;

    public RtmAnalyzer(string uri, SymbolIndex index)
    {
        _uri = uri;
        _index = index;
    }

    public override void EnterOverlay_name(RtmParser.Overlay_nameContext context)
    {
        Define(context.ID()?.Symbol, symbol => new OverlaySymbol(symbol.Name, _uri, symbol.Range, symbol.Range));
    }

    public override void EnterProc(RtmParser.ProcContext context)
    {
        Define(context.ID()?.Symbol, symbol => new ProcedureSymbol(symbol.Name, _uri, symbol.Range, symbol.Range));
    }

    public override void EnterData_field(RtmParser.Data_fieldContext context)
    {
        var token = context.DATA_ID_OR_MASK()?.FirstOrDefault()?.Symbol
                    ?? context.FIELD_ID()?.FirstOrDefault()?.Symbol
                    ?? context.FILL()?.Symbol;

        Define(token, symbol =>
        {
            var field = new FieldSymbol(symbol.Name, _uri, symbol.Range, symbol.Range)
            {
                EditMask = ParseEditMask(context),
                Dimensions = ParseDimensions(context).ToArray()
            };
            if (context.EQUAL() is not null)
            {
                field.AssignedFrom =
                    FirstTerminalText(context.DATA_ID_OR_MASK())
                    ?? FirstTerminalText(context.FIELD_ID());
            }

            return field;
        });
    }

    public override void EnterParameter_list(RtmParser.Parameter_listContext context)
    {
        foreach (var assignable in context.assignable())
        {
            Define(assignable.Start, symbol => new ParameterSymbol(symbol.Name, _uri, symbol.Range, symbol.Range));
        }
    }

    public override void EnterFiles(RtmParser.FilesContext context)
    {
        foreach (var id in context.ID())
            Define(id.Symbol, symbol => new FileSymbol(symbol.Name, _uri, symbol.Range, symbol.Range));
    }

    public override void EnterExt(RtmParser.ExtContext context)
    {
        foreach (var id in context.ID())
            Define(id.Symbol, symbol => new ExternalSymbol(symbol.Name, _uri, symbol.Range, symbol.Range));
    }

    public override void EnterIdentifier_expression(RtmParser.Identifier_expressionContext context)
    {
        Reference(context.Start);
    }

    public override void EnterAssignable(RtmParser.AssignableContext context)
    {
        Reference(context.Start);
    }

    public override void EnterFunction_call(RtmParser.Function_callContext context)
    {
        var id = context.ID();
        if (id is null) return;
        Reference(id.Symbol);
    }

    public override void EnterCopy_mask(RtmParser.Copy_maskContext context)
    {
        var token = context.ID()?.Symbol ?? context.DATA_ID_OR_MASK()?.Symbol;
        Reference(token);
    }

    private void Define(IToken? token, Func<PendingSymbol, RtmSymbol> createSymbol)
    {
        if (token is null || token.Type == TokenConstants.EOF) return;
        var name = CleanName(token.Text);
        if (string.IsNullOrWhiteSpace(name)) return;

        var range = SourceRange.FromToken(token);
        _index.AddDefinition(createSymbol(new PendingSymbol(name, range)));
    }

    private void Reference(IToken? token)
    {
        if (token is null || token.Type == TokenConstants.EOF) return;
        var name = CleanName(token.Text);
        if (string.IsNullOrWhiteSpace(name)) return;

        var range = SourceRange.FromToken(token);
        _index.AddReference(name, new Location(_uri, range));
    }

    public static string CleanName(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Trim().TrimStart('\'').TrimStart('$');
    }

    private readonly record struct PendingSymbol(string Name, TextRange Range);

    private static string? FirstTerminalText(ITerminalNode[] nodes)
    {
        return nodes is { Length: > 0 } ? nodes[0].Symbol.Text : null;
    }

    private static string ParseFieldName(RtmParser.Data_fieldContext context)
    {
        return FirstTerminalText(context.DATA_ID_OR_MASK())
               ?? FirstTerminalText(context.FIELD_ID())
               ?? context.FILL()?.Symbol.Text
               ?? "FIELD";
    }

    private static string? ParseEditMask(RtmParser.Data_fieldContext context)
    {
        return context.edit_mask()?.GetText()
               ?? context.group_mask()?.GetText();
    }


    private static IEnumerable<int> ParseDimensions(RtmParser.Data_fieldContext context)
    {
        var numbers = context.NUMERIC_LITERAL();

        foreach (var n in numbers)
        {
            if (int.TryParse(n.Symbol.Text, out var value))
                yield return value;
        }
    }


    private static List<string> ParseParameters(RtmParser.Parameter_listContext? context)
    {
        var result = new List<string>();

        if (context?.children is null)
            return result;

        foreach (var child in context.children)
        {
            switch (child)
            {
                case RtmParser.AssignableContext assignable:
                    result.Add(assignable.GetText());
                    break;
                case ITerminalNode terminal when
                    terminal.Symbol.Type == RtmParser.STAR:
                    result.Add("*");
                    break;
            }
        }

        return result;
    }
}