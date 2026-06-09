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
    [JsonPropertyName("severity")] public int Severity { get; set; } = 1; // 1=Error, 2=Warning, 3=Info, 4=Hint
    [JsonPropertyName("source")] public string Source { get; set; } = "rtm";
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
}

public sealed class PublishDiagnosticsParams
{
    [JsonPropertyName("uri")] public string Uri { get; set; } = string.Empty;
    [JsonPropertyName("diagnostics")] public List<Diagnostic> Diagnostics { get; set; } = [];
}
