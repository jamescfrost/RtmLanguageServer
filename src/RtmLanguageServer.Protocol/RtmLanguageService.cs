using RtmLanguageServer.Core.Common;
using RtmLanguageServer.Core.Parsing;
using RtmLanguageServer.Core.Symbols;

namespace RtmLanguageServer.Protocol;

public sealed class RtmLanguageService
{
    private readonly DocumentStore _documents;

    public RtmLanguageService(DocumentStore documents) => _documents = documents;

    public IReadOnlyList<Location> GetDefinition(TextDocumentPositionParams request)
    {
        var symbol = ResolveSymbol(request);
        return symbol is not null ? [symbol.Location] : [];
    }

    public IReadOnlyList<Location> FindReferences(ReferenceParams request)
    {
        var symbol = ResolveSymbol(request);
        if (symbol is null)
            return [];

        var refs = new List<Location>();
        foreach (var doc in _documents.All)
            refs.AddRange(doc.Index.FindReferences(symbol, request.Context.IncludeDeclaration));

        refs.Sort(LocationEqualityComparer.Instance);
        return refs.Distinct(LocationEqualityComparer.Instance).ToList();
    }

    public Hover? Hover(TextDocumentPositionParams request)
    {
        var symbol = ResolveSymbol(request);
        if (symbol is null)
            return null;

        return new Hover
        {
            Contents = new MarkupContent
            {
                Kind = "markdown",
                Value = BuildHoverMarkdown(symbol)
            },
            Range = symbol.Location.Range
        };
    }

    public SemanticTokens GetSemanticTokens(SemanticTokensParams request)
    {
        if (!_documents.TryGet(request.TextDocument.Uri, out var document) || document is null)
            return new SemanticTokens();

        var builder = new SemanticTokensBuilder();

        // Process all symbols and references in the document
        foreach (var symbol in document.Index.AllDefinitions)
        {
            // Add the symbol definition
            builder.AddToken(symbol.Location.Range, GetTokenType(symbol), isDefinition: true);
        }

        // Process all references
        foreach (var reference in document.Index.AllReferences)
        {
            if (reference.ResolvedSymbol is not null)
            {
                builder.AddToken(reference.Location.Range, GetTokenType(reference.ResolvedSymbol), isDefinition: false);
            }
        }

        return builder.Build();
    }

    private static string GetTokenType(RtmSymbol symbol) => symbol switch
    {
        ProgramSymbol => SemanticTokenTypes.Method,           // $PROG → method color (check before ProcedureSymbol)
        ExternalSymbol => SemanticTokenTypes.Class,           // Overlays → green/teal like classes
        ProcedureSymbol => SemanticTokenTypes.Function,       // Procedures → function color
        FieldSymbol => SemanticTokenTypes.Property,           // Fields → property color
        FileSymbol => SemanticTokenTypes.Namespace,           // Files → namespace color
        OverlaySymbol => SemanticTokenTypes.Namespace,        // Overlay containers → namespace color
        _ => SemanticTokenTypes.Variable                      // Fallback
    };

    private RtmSymbol? ResolveSymbol(TextDocumentPositionParams request)
    {
        if (!_documents.TryGet(request.TextDocument.Uri, out var document) || document is null)
            return null;

        return document.ResolveSymbolAt(request.Position, _documents.All);
    }

    private static string BuildHoverMarkdown(RtmSymbol symbol)
    {
        var lines = new List<string>
        {
            $"**{ToTitle(symbol.Label)}** `{symbol.Name}`"
        };

        var detail = symbol.GetMarkdown();
        if (!string.IsNullOrWhiteSpace(detail))
            lines.Add(detail);

        if (symbol is not OverlaySymbol && symbol.Overlay is not null)
            lines.Add($"Overlay: `{symbol.Overlay.Name}`");

        if (symbol is OverlaySymbol overlayWithChildren && overlayWithChildren.Children.Count > 0)
        {
            var childSummary = overlayWithChildren.Children
                .GroupBy(c => c.Label)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Count()} {g.Key}");

            lines.Add($"Children: {string.Join(", ", childSummary)}");
        }

        lines.Add($"Defined in `{DisplayUri(symbol.Location.Uri)}`");

        return string.Join("\n\n", lines);
    }

    private static string ToTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Symbol";

        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    private static string DisplayUri(string uri) => RtmUri.DisplayName(uri);
}

internal sealed class SemanticTokensBuilder
{
    private readonly List<SemanticToken> _tokens = [];
    private static readonly Dictionary<string, int> _tokenTypeMap = BuildTokenTypeMap();
    private static readonly Dictionary<string, int> _tokenModifierMap = BuildTokenModifierMap();

    private static Dictionary<string, int> BuildTokenTypeMap()
    {
        var types = new[]
        {
            SemanticTokenTypes.Namespace,
            SemanticTokenTypes.Class,
            SemanticTokenTypes.Enum,
            SemanticTokenTypes.Interface,
            SemanticTokenTypes.Struct,
            SemanticTokenTypes.TypeParameter,
            SemanticTokenTypes.Type,
            SemanticTokenTypes.Parameter,
            SemanticTokenTypes.Variable,
            SemanticTokenTypes.Property,
            SemanticTokenTypes.EnumMember,
            SemanticTokenTypes.Decorator,
            SemanticTokenTypes.Event,
            SemanticTokenTypes.Function,
            SemanticTokenTypes.Method,
            SemanticTokenTypes.Macro,
            SemanticTokenTypes.Label,
            SemanticTokenTypes.Comment,
            SemanticTokenTypes.String,
            SemanticTokenTypes.Keyword,
            SemanticTokenTypes.Number,
            SemanticTokenTypes.Regexp,
            SemanticTokenTypes.Operator
        };

        return types.Select((type, index) => (type, index)).ToDictionary(x => x.type, x => x.index);
    }

    private static Dictionary<string, int> BuildTokenModifierMap()
    {
        var modifiers = new[]
        {
            SemanticTokenModifiers.Declaration,
            SemanticTokenModifiers.Definition,
            SemanticTokenModifiers.Readonly,
            SemanticTokenModifiers.Static,
            SemanticTokenModifiers.Deprecated,
            SemanticTokenModifiers.Abstract,
            SemanticTokenModifiers.Async,
            SemanticTokenModifiers.Modification,
            SemanticTokenModifiers.Documentation,
            SemanticTokenModifiers.DefaultLibrary
        };

        return modifiers.Select((modifier, index) => (modifier, index)).ToDictionary(x => x.modifier, x => x.index);
    }

    public static SemanticTokensLegend GetLegend()
    {
        return new SemanticTokensLegend
        {
            TokenTypes = _tokenTypeMap.OrderBy(kv => kv.Value).Select(kv => kv.Key).ToList(),
            TokenModifiers = _tokenModifierMap.OrderBy(kv => kv.Value).Select(kv => kv.Key).ToList()
        };
    }

    public void AddToken(TextRange range, string tokenType, bool isDefinition)
    {
        if (!_tokenTypeMap.TryGetValue(tokenType, out var typeIndex))
            return;

        var modifierBits = 0;
        if (isDefinition && _tokenModifierMap.TryGetValue(SemanticTokenModifiers.Declaration, out var declIndex))
            modifierBits |= 1 << declIndex;

        _tokens.Add(new SemanticToken
        {
            Line = range.Start.Line,
            StartChar = range.Start.Character,
            Length = range.End.Character - range.Start.Character,
            TokenType = typeIndex,
            TokenModifiers = modifierBits
        });
    }

    public SemanticTokens Build()
    {
        // Sort tokens by line, then by character
        var sorted = _tokens.OrderBy(t => t.Line).ThenBy(t => t.StartChar).ToList();

        var data = new List<int>();
        var prevLine = 0;
        var prevChar = 0;

        foreach (var token in sorted)
        {
            var deltaLine = token.Line - prevLine;
            var deltaChar = deltaLine == 0 ? token.StartChar - prevChar : token.StartChar;

            data.Add(deltaLine);
            data.Add(deltaChar);
            data.Add(token.Length);
            data.Add(token.TokenType);
            data.Add(token.TokenModifiers);

            prevLine = token.Line;
            prevChar = token.StartChar;
        }

        return new SemanticTokens { Data = data };
    }

    private sealed class SemanticToken
    {
        public int Line { get; init; }
        public int StartChar { get; init; }
        public int Length { get; init; }
        public int TokenType { get; init; }
        public int TokenModifiers { get; init; }
    }
}
