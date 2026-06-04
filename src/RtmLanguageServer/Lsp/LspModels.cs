using RtmLanguageServer.Analysis.Core;
using System.Text.Json.Serialization;

namespace RtmLanguageServer.Lsp;

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