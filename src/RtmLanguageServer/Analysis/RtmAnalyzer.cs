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


    private OverlaySymbol? _currentOverlay;

    private ProgramSymbol? _currentProg;

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
                _currentOverlay = new OverlaySymbol(
                    symbol.Name,
                    _uri,
                    symbol.Range,
                    symbol.Range);
                return _currentOverlay;
            });
    }

    public override void ExitOverlay(RtmParser.OverlayContext context)
    {
        _currentOverlay = null;
    }

    public override void EnterProg(RtmParser.ProgContext context)
    {
        Define(
            context.DOLLAR_PROG()?.Symbol,
            symbol =>
            {
                _currentProg = new ProgramSymbol(
                    "$PROG",
                    _uri,
                    symbol.Range,
                    symbol.Range)
                {
                    Parameters = ParseParameters(context.parameter_list())
                };
                return _currentProg;
            });
    }

    public override void ExitProg(RtmParser.ProgContext context)
    {
        _currentProg = null;
    }

    public override void EnterProc(RtmParser.ProcContext context)
    {
        Define(
            context.ID()?.Symbol,
            symbol => new ProcedureSymbol(
                symbol.Name,
                _uri,
                symbol.Range,
                symbol.Range)
            {
                Parameters = ParseParameters(context.parameter_list())
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
                AssignedFrom = ParseAssignedFrom(context)
            });
    }

    public override void EnterParameter_list(RtmParser.Parameter_listContext context)
    {
        foreach (var assignable in context.assignable())
        {
            Define(
                assignable.Start,
                symbol => new ParameterSymbol(symbol.Name, _uri, symbol.Range, symbol.Range));
        }
    }

    public override void EnterFiles(RtmParser.FilesContext context)
    {
        foreach (var id in context.ID())
        {
            Define(
                id.Symbol,
                symbol => new FileSymbol(symbol.Name, _uri, symbol.Range, symbol.Range));
        }
    }

    public override void EnterExt(RtmParser.ExtContext context)
    {
        foreach (var id in context.ID())
        {
            Define(
                id.Symbol,
                symbol => new ExternalSymbol(symbol.Name, _uri, symbol.Range, symbol.Range));
        }
    }

    public override void EnterIdentifier_expression(RtmParser.Identifier_expressionContext context)
    {
        Reference(context.Start);
    }

    public override void EnterFunction_call(RtmParser.Function_callContext context)
    {
        // function_call: (ID | OVERLAY ID) call_parameter_list;
        // The function/procedure name is the last ID in this rule.
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
        Func<PendingSymbol, RtmSymbol> createSymbol)
    {
        if (token is null || token.Type == TokenConstants.EOF)
            return;

        var name = CleanName(token.Text);
        if (string.IsNullOrWhiteSpace(name))
            return;

        var range = SourceRange.FromToken(token);
        var symbol = createSymbol(new PendingSymbol(name, range));

        if (_currentOverlay is not null && symbol is not OverlaySymbol)
        {
            symbol.Overlay = _currentOverlay;
            _currentOverlay.Children.Add(symbol);
            if (_currentProg is not null && symbol is not ProgramSymbol)
            {
                _currentProg.Children.Add(symbol);
            }
        }

        _index.AddDefinition(symbol);
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

    private static string? LastTerminalText(ITerminalNode[] nodes)
    {
        return nodes.Length > 0 ? nodes[^1].Symbol.Text : null;
    }

    private static string? ParseAssignedFrom(RtmParser.Data_fieldContext context)
    {
        if (context.EQUAL() is null)
            return null;

        // For "A = B", ANTLR exposes both A and B through the same token accessors.
        // The assigned-from value is therefore the last field-like token in the rule.
        return LastTerminalText(context.DATA_ID_OR_MASK())
               ?? LastTerminalText(context.FIELD_ID());
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

    private static IReadOnlyList<string> ParseParameters(RtmParser.Parameter_listContext? context)
    {
        var result = new List<string>();

        if (context?.children is null)
            return result;

        foreach (var child in context.children)
        {
            switch (child)
            {
                case RtmParser.AssignableContext assignable:
                    var name = CleanName(assignable.GetText());
                    if (!string.IsNullOrWhiteSpace(name))
                        result.Add(name);
                    break;

                case ITerminalNode terminal when terminal.Symbol.Type == RtmParser.STAR:
                    result.Add("*");
                    break;
            }
        }

        return result;
    }
}