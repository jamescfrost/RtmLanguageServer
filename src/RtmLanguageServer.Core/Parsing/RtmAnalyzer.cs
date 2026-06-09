using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using RtmLanguageServer.Core.Analysis;
using RtmLanguageServer.Core.Common;
using RtmLanguageServer.Core.Symbols;
using RtmLanguageServer.Core.Generated;

namespace RtmLanguageServer.Core.Parsing;

public sealed class RtmAnalyzer : RtmParserBaseListener
{
    private readonly string _uri;
    private readonly SymbolIndex _index;
    private readonly RtmScope _globalScope = new();
    private readonly RtmPreprocessedSource _preprocessed;

    private RtmScope _currentScope;
    private OverlaySymbol? _currentOverlay;
    private ProgramSymbol? _currentProg;
    private ProcedureSymbol? _currentProcedure;

    public RtmAnalyzer(string uri, SymbolIndex index, RtmPreprocessedSource preprocessed)
    {
        _uri = uri;
        _index = index;
        _currentScope = _globalScope;
        _preprocessed = preprocessed;
    }

    public override void EnterName_definition(RtmParser.Name_definitionContext context)
    {
        _currentOverlay = null;
        _currentProg = null;
        _currentProcedure = null;
        _currentScope = _globalScope;

        var id = context.ID();
        if (id is null)
            return;

        var name = CleanName(id.Symbol.Text);
        if (string.IsNullOrWhiteSpace(name))
            return;

        var symbol = new NameIncludeSymbol(name, LocationFromToken(id.Symbol))
        {
            Scope = _currentScope
        };

        _index.AddDefinition(symbol);
    }

    public override void EnterOverlay_name(RtmParser.Overlay_nameContext context)
    {
        Define(
            context.ID()?.Symbol,
            symbol => new OverlaySymbol(symbol.Name, symbol.Location),
            afterDefine: symbol =>
            {
                _currentOverlay = (OverlaySymbol)symbol;
                _currentScope = new RtmScope(_globalScope, symbol);
            });
    }

    public override void ExitOverlay(RtmParser.OverlayContext context)
    {
        _currentOverlay = null;
        _currentProg = null;
        _currentProcedure = null;
        _currentScope = _globalScope;
    }

    public override void EnterProg(RtmParser.ProgContext context)
    {
        Define(
            context.DOLLAR_PROG()?.Symbol,
            symbol => new ProgramSymbol("$PROG", symbol.Location),
            afterDefine: symbol =>
            {
                _currentProg = (ProgramSymbol)symbol;
                _currentProcedure = _currentProg;
                _currentScope = new RtmScope(_currentScope, symbol);
                DefineParameters(context.parameter_list(), _currentProg);
            });
    }

    public override void ExitProg(RtmParser.ProgContext context)
    {
        _currentScope = _currentScope.Parent ?? _globalScope;
        _currentProg = null;
        _currentProcedure = null;
    }

    public override void EnterProc(RtmParser.ProcContext context)
    {
        Define(
            context.ID()?.Symbol,
            symbol => new ProcedureSymbol(symbol.Name, symbol.Location),
            afterDefine: symbol =>
            {
                _currentProcedure = (ProcedureSymbol)symbol;
                _currentScope = new RtmScope(_currentScope, symbol);
                DefineParameters(context.parameter_list(), _currentProcedure);
            });
    }

    public override void ExitProc(RtmParser.ProcContext context)
    {
        _currentScope = _currentScope.Parent ?? _globalScope;
        _currentProcedure = null;
    }

    public override void EnterAbort(RtmParser.AbortContext context)
    {
        _currentScope = new RtmScope(_currentScope, _currentOverlay);
    }

    public override void ExitAbort(RtmParser.AbortContext context)
    {
        _currentScope = _currentScope.Parent ?? _globalScope;
    }

    public override void EnterData_field(RtmParser.Data_fieldContext context)
    {
        var token =
            context.DATA_ID_OR_MASK()?.FirstOrDefault()?.Symbol
            ?? context.FIELD_ID()?.FirstOrDefault()?.Symbol
            ?? context.FILL()?.Symbol;

        Define(
            token,
            symbol => new FieldSymbol(symbol.Name, symbol.Location)
            {
                EditMask = ParseEditMask(context),
                Dimensions = ParseDimensions(context).ToArray(),
                AssignedFrom = ParseAssignedFrom(context)
            });
    }

    public override void EnterFiles(RtmParser.FilesContext context)
    {
        foreach (var id in context.ID())
        {
            Define(
                id.Symbol,
                symbol => new FileSymbol(symbol.Name, symbol.Location));
        }
    }

    public override void EnterExt(RtmParser.ExtContext context)
    {
        foreach (var id in context.ID())
        {
            Define(
                id.Symbol,
                symbol => new ExternalSymbol(symbol.Name, symbol.Location));
        }
    }

    public override void EnterName_include(RtmParser.Name_includeContext context)
    {
        var sourceName = context.INCLUDE_FILENAME()?.Symbol.Text ?? "*";
        var includeName = context.INCLUDE_NAME()?.Symbol.Text ?? "";

        var reference = new IncludeReference
        {
            Name = includeName,
            SourceName = sourceName,
            IncludeName = includeName,
            Location = LocationFromToken(context.Start),
            Overlay = _currentOverlay
        };

        _index.AddReference(reference);
    }

    public override void EnterIdentifier_expression(RtmParser.Identifier_expressionContext context)
    {
        Reference(
            context.Start,
            reference => new FieldReference
            {
                Name = reference.Name,
                Location = reference.Location,
                Overlay = _currentOverlay
            });
    }

    public override void EnterAssignable(RtmParser.AssignableContext context)
    {
        // Assignment targets and return parameters are references too. They do not
        // pass through identifier_expression, so without this pass “find all
        // references” misses common overlay-body usages such as FIELD = VALUE.
        if (IsProcedureSignatureParameter(context))
            return;

        Reference(
            context.Start,
            reference => new FieldReference
            {
                Name = reference.Name,
                Location = reference.Location,
                Overlay = _currentOverlay
            });
    }

    public override void EnterFunction_call(RtmParser.Function_callContext context)
    {
        // OVERLAY NAME(...) is a reference to an overlay, not a procedure named
        // OVERLAY. Index the target ID so “find all references” on an overlay
        // includes calls from all parsed overlay bodies.
        if (context.OVERLAY() is not null)
        {
            var overlayId = context.ID()?.Symbol;
            if (overlayId is not null)
            {
                Reference(
                    overlayId,
                    reference => new OverlayReference
                    {
                        Name = reference.Name,
                        Location = reference.Location,
                        Overlay = _currentOverlay
                    });
            }

            return;
        }

        Reference(
            context.Start,
            reference => new ProcedureReference
            {
                Name = reference.Name,
                Location = reference.Location,
                Overlay = _currentOverlay
            });
    }

    private void Define(
        IToken? token,
        Func<PendingSymbol, RtmSymbol> createSymbol,
        bool indexByName = true,
        Action<RtmSymbol>? afterDefine = null)
    {
        if (token is null || token.Type == TokenConstants.EOF)
            return;

        var name = CleanName(token.Text);
        if (string.IsNullOrWhiteSpace(name))
            return;

        var location = LocationFromToken(token);
        var symbol = createSymbol(new PendingSymbol(name, location));
        symbol.Scope = _currentScope;

        if (_currentOverlay is not null && symbol is not OverlaySymbol)
            symbol.Overlay = _currentOverlay;

        _index.AddDefinition(symbol, indexByName);
        afterDefine?.Invoke(symbol);
    }

    private void DefineParameters(RtmParser.Parameter_listContext? context, ProcedureSymbol owner)
    {
        if (context?.children is null)
            return;

        foreach (var child in context.children)
        {
            switch (child)
            {
                case RtmParser.AssignableContext assignable:
                    AddProcedureParameter(owner, assignable.Start);
                    break;

                case ITerminalNode terminal when terminal.Symbol.Type == RtmParser.STAR:
                    owner.Parameters.Add(new FieldReference
                    {
                        Name = "*",
                        Location = LocationFromToken(terminal.Symbol),
                        Overlay = _currentOverlay
                    });
                    break;
            }
        }
    }

    private void AddProcedureParameter(ProcedureSymbol owner, IToken token)
    {
        var name = CleanName(token.Text);
        if (string.IsNullOrWhiteSpace(name))
            return;

        var reference = new FieldReference
        {
            Name = name,
            Location = LocationFromToken(token),
            Overlay = _currentOverlay
        };

        owner.Parameters.Add(reference);

        // PROG/PROC signature parameters are usages of fields too. They are
        // deliberately skipped by EnterAssignable to avoid double-indexing, so
        // index them here explicitly for find-all-references on FieldSymbol.
        _index.AddReference(reference);
    }

    private static bool IsProcedureSignatureParameter(RtmParser.AssignableContext context)
    {
        if (context.Parent is not RtmParser.Parameter_listContext parameterList)
            return false;

        return parameterList.Parent is RtmParser.ProgContext
            or RtmParser.ProcContext
            or RtmParser.AbortContext;
    }

    private void Reference(IToken token, Func<PendingReference, RtmReference> createReference)
    {
        var name = CleanName(token.Text);
        if (string.IsNullOrWhiteSpace(name))
            return;

        var location = LocationFromToken(token);
        var reference = createReference(new PendingReference(name, location));

        _index.AddReference(reference);
    }

    private TextRange RangeFromToken(IToken token)
    {
        return _preprocessed.MapRange(token.GetRange());
    }

    private Location LocationFromToken(IToken token)
    {
        return _preprocessed.MapLocation(_uri, token.GetRange());
    }

    public static string CleanName(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Trim().TrimStart('\'').TrimStart('$');
    }

    private readonly record struct PendingSymbol(string Name, Location Location)
    {
        public TextRange Range => Location.Range;
    }

    private readonly record struct PendingReference(string Name, Location Location);

    private static string? LastTerminalText(ITerminalNode[] nodes)
    {
        return nodes.Length > 0 ? nodes[^1].Symbol.Text : null;
    }

    private static string? ParseAssignedFrom(RtmParser.Data_fieldContext context)
    {
        if (context.EQUAL() is null)
            return null;

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
}
