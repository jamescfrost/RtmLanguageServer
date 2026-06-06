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

    private readonly Stack<OverlaySymbol> _overlayStack = new();

    private OverlaySymbol? CurrentOverlay =>
        _overlayStack.Count > 0 ? _overlayStack.Peek() : null;

    public RtmAnalyzer(string uri, SymbolIndex index)
    {
        _uri = uri;
        _index = index;
    }

    public override void EnterOverlay_name(RtmParser.Overlay_nameContext context)
    {
        Define(
            context.ID()?.Symbol,
            symbol =>
            {
                var overlay = new OverlaySymbol(
                    symbol.Name,
                    _uri,
                    symbol.Range,
                    symbol.Range);

                _overlayStack.Push(overlay);

                return overlay;
            },
            alsoReference: true);
    }

    public override void ExitOverlay(RtmParser.OverlayContext context)
    {
        if (_overlayStack.Count > 0)
            _overlayStack.Pop();
    }

    public override void EnterProg(RtmParser.ProgContext context)
    {
        Define(
            context.DOLLAR_PROG()?.Symbol,
            symbol =>
            {
                var prog = new ProgramSymbol(
                    "$PROG",
                    _uri,
                    symbol.Range,
                    symbol.Range);

                prog.Parameters = ParseParameters(context.parameter_list());

                return prog;
            },
            alsoReference: true);
    }

    public override void EnterProc(RtmParser.ProcContext context)
    {
        Define(
            context.ID()?.Symbol,
            symbol =>
            {
                var proc = new ProcedureSymbol(
                    symbol.Name,
                    _uri,
                    symbol.Range,
                    symbol.Range)
                {
                    Parameters = ParseParameters(context.parameter_list())
                };

                return proc;
            });
    }

    public override void EnterData_field(RtmParser.Data_fieldContext context)
    {
        var token =
            context.DATA_ID_OR_MASK()?.FirstOrDefault()?.Symbol
            ?? context.FIELD_ID()?.FirstOrDefault()?.Symbol
            ?? context.FILL()?.Symbol;

        Define(
            token,
            symbol => new FieldSymbol(symbol.Name, _uri, symbol.Range, symbol.Range)
            {
                EditMask = ParseEditMask(context),
                Dimensions = ParseDimensions(context).ToArray(),
                AssignedFrom = context.EQUAL() is not null
                    ? ParseFieldName(context)
                    : null
            },
            alsoReference: true);
    }

    public override void EnterParameter_list(RtmParser.Parameter_listContext context)
    {
        foreach (var assignable in context.assignable())
        {
            Define(
                assignable.Start,
                symbol => new ParameterSymbol(symbol.Name, _uri, symbol.Range, symbol.Range),
                alsoReference: true);
        }
    }

    public override void EnterFiles(RtmParser.FilesContext context)
    {
        foreach (var id in context.ID())
        {
            Define(
                id.Symbol,
                symbol => new FileSymbol(symbol.Name, _uri, symbol.Range, symbol.Range),
                alsoReference: true);
        }
    }

    public override void EnterExt(RtmParser.ExtContext context)
    {
        foreach (var id in context.ID())
        {
            Define(
                id.Symbol,
                symbol => new ExternalSymbol(symbol.Name, _uri, symbol.Range, symbol.Range),
                alsoReference: true);
        }
    }

    public override void EnterIdentifier_expression(RtmParser.Identifier_expressionContext context)
    {
        Reference(context.Start);
    }

    public override void EnterFunction_call(RtmParser.Function_callContext context)
    {
        Reference(context.ID()?.Symbol);
    }

    public override void EnterCopy_mask(RtmParser.Copy_maskContext context)
    {
        var token =
            context.ID()?.Symbol
            ?? context.DATA_ID_OR_MASK()?.Symbol;

        Reference(token);
    }

    private void Define(
        IToken? token,
        Func<PendingSymbol, RtmSymbol> createSymbol,
        bool alsoReference = false)
    {
        if (token is null || token.Type == TokenConstants.EOF)
            return;

        var name = CleanName(token.Text);
        if (string.IsNullOrWhiteSpace(name))
            return;

        var range = SourceRange.FromToken(token);
        var symbol = createSymbol(new PendingSymbol(name, range));

        _index.AddDefinition(symbol);

        if (CurrentOverlay is not null && symbol is not OverlaySymbol)
        {
            symbol.Overlay = CurrentOverlay;
            CurrentOverlay.Children.Add(symbol);
        }

        if (alsoReference)
            _index.AddReference(name, new Location(_uri, range));
    }

    private void Reference(IToken? token)
    {
        if (token is null || token.Type == TokenConstants.EOF)
            return;

        var name = CleanName(token.Text);
        if (string.IsNullOrWhiteSpace(name))
            return;

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
        return nodes.Length > 0 ? nodes[0].Symbol.Text : null;
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
        foreach (var number in context.NUMERIC_LITERAL())
        {
            if (int.TryParse(number.Symbol.Text, out var value))
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