using RtmLanguageServer.Core.Common;
using System.Text.Json.Serialization;

namespace RtmLanguageServer.Protocol;

public sealed class InitializeParams
{
    [JsonPropertyName("rootUri")] public string? RootUri { get; set; }
    [JsonPropertyName("rootPath")] public string? RootPath { get; set; }
    [JsonPropertyName("workspaceFolders")] public List<WorkspaceFolder>? WorkspaceFolders { get; set; }
}

public sealed class WorkspaceFolder
{
    [JsonPropertyName("uri")] public string Uri { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
}

public sealed class TextDocumentItem
{
    [JsonPropertyName("uri")] public string Uri { get; set; } = string.Empty;
    [JsonPropertyName("languageId")] public string LanguageId { get; set; } = "rtm";
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
}

public sealed class VersionedTextDocumentIdentifier
{
    [JsonPropertyName("uri")] public string Uri { get; set; } = string.Empty;
    [JsonPropertyName("version")] public int? Version { get; set; }
}

public sealed class TextDocumentIdentifier
{
    [JsonPropertyName("uri")] public string Uri { get; set; } = string.Empty;
}

public class TextDocumentPositionParams
{
    [JsonPropertyName("textDocument")] public TextDocumentIdentifier TextDocument { get; set; } = new();
    [JsonPropertyName("position")] public TextPosition Position { get; set; } = new(0, 0);
}

public sealed class DidOpenTextDocumentParams
{
    [JsonPropertyName("textDocument")] public TextDocumentItem TextDocument { get; set; } = new();
}

public sealed class TextDocumentContentChangeEvent
{
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
}

public sealed class DidChangeTextDocumentParams
{
    [JsonPropertyName("textDocument")] public VersionedTextDocumentIdentifier TextDocument { get; set; } = new();
    [JsonPropertyName("contentChanges")] public List<TextDocumentContentChangeEvent> ContentChanges { get; set; } = [];
}

public sealed class ReferenceParams : TextDocumentPositionParams
{
    [JsonPropertyName("context")] public ReferenceContext Context { get; set; } = new();
}

public sealed class ReferenceContext
{
    [JsonPropertyName("includeDeclaration")]
    public bool IncludeDeclaration { get; set; }
}

public sealed class MarkupContent
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = "markdown";
    [JsonPropertyName("value")] public string Value { get; set; } = string.Empty;
}

public sealed class Hover
{
    [JsonPropertyName("contents")] public MarkupContent Contents { get; set; } = new();
    [JsonPropertyName("range")] public TextRange? Range { get; set; }
}

public sealed class Diagnostic
{
    [JsonPropertyName("range")] public TextRange Range { get; set; } = new(new TextPosition(0, 0), new TextPosition(0, 0));
    [JsonPropertyName("severity")] public int Severity { get; set; } = (int)DiagnosticSeverity.Error;
    [JsonPropertyName("source")] public string Source { get; set; } = "rtm";
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
}

public sealed class PublishDiagnosticsParams
{
    [JsonPropertyName("uri")] public string Uri { get; set; } = string.Empty;
    [JsonPropertyName("diagnostics")] public List<Diagnostic> Diagnostics { get; set; } = [];
}

public sealed class SemanticTokensParams
{
    [JsonPropertyName("textDocument")] public TextDocumentIdentifier TextDocument { get; set; } = new();
}

public sealed class SemanticTokens
{
    [JsonPropertyName("data")] public List<int> Data { get; set; } = [];
}

public sealed class SemanticTokensLegend
{
    [JsonPropertyName("tokenTypes")] public List<string> TokenTypes { get; set; } = [];
    [JsonPropertyName("tokenModifiers")] public List<string> TokenModifiers { get; set; } = [];
}

public static class SemanticTokenTypes
{
    public const string Namespace = "namespace";
    public const string Class = "class";
    public const string Enum = "enum";
    public const string Interface = "interface";
    public const string Struct = "struct";
    public const string TypeParameter = "typeParameter";
    public const string Type = "type";
    public const string Parameter = "parameter";
    public const string Variable = "variable";
    public const string Property = "property";
    public const string EnumMember = "enumMember";
    public const string Decorator = "decorator";
    public const string Event = "event";
    public const string Function = "function";
    public const string Method = "method";
    public const string Macro = "macro";
    public const string Label = "label";
    public const string Comment = "comment";
    public const string String = "string";
    public const string Keyword = "keyword";
    public const string Number = "number";
    public const string Regexp = "regexp";
    public const string Operator = "operator";
}

public static class SemanticTokenModifiers
{
    public const string Declaration = "declaration";
    public const string Definition = "definition";
    public const string Readonly = "readonly";
    public const string Static = "static";
    public const string Deprecated = "deprecated";
    public const string Abstract = "abstract";
    public const string Async = "async";
    public const string Modification = "modification";
    public const string Documentation = "documentation";
    public const string DefaultLibrary = "defaultLibrary";
}
