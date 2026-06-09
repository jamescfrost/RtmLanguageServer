using System.Text;
using System.Text.Json;

namespace RtmLanguageServer.Lsp;

public sealed class JsonRpcServer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Stream _input;
    private readonly Stream _output;
    private readonly DocumentStore _documents = new();
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private readonly RtmLanguageService _service;
    private int _nextRequestId = 1;
    private string? _activeProgressToken;

    public JsonRpcServer(Stream input, Stream output)
    {
        _input = input;
        _output = output;
        _documents.Status += PublishStatus;
        _service = new RtmLanguageService(_documents);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var payload = await ReadMessageAsync(cancellationToken).ConfigureAwait(false);
            if (payload is null) break;
            await HandleMessageAsync(payload, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleMessageAsync(string payload, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var method = root.TryGetProperty("method", out var methodEl) ? methodEl.GetString() : null;
        var hasId = root.TryGetProperty("id", out var idEl);
        var id = hasId ? idEl.Clone() : default;
        var paramsEl = root.TryGetProperty("params", out var p) ? p : default;

        // Response to a server-initiated request such as window/workDoneProgress/create.
        // The current server does not need to block waiting for these responses.
        if (method is null)
            return;

        try
        {
            switch (method)
            {
                case "initialize":
                    {
                        var args = paramsEl.ValueKind == JsonValueKind.Undefined
                            ? new InitializeParams()
                            : paramsEl.Deserialize<InitializeParams>(JsonOptions) ?? new InitializeParams();

                        var roots = args.WorkspaceFolders is { Count: > 0 }
                            ? args.WorkspaceFolders.Select(f => f.Uri)
                            : new[] { args.RootUri ?? args.RootPath };

                        await SendResultAsync(id, new
                        {
                            capabilities = new
                            {
                                textDocumentSync = 1,
                                definitionProvider = true,
                                referencesProvider = true,
                                hoverProvider = true
                            },
                            serverInfo = new { name = "rtm-language-server", version = "0.1.0" }
                        }, cancellationToken).ConfigureAwait(false);

                        // Send visible progress only after the initialize response. Some
                        // clients ignore UI notifications sent before initialization has
                        // completed, which made workspace preload look silent.
                        await BeginProgressAsync("RTM: Loading workspace", cancellationToken).ConfigureAwait(false);
                        try
                        {
                            foreach (var workspaceRoot in roots)
                                _documents.LoadWorkspace(workspaceRoot);
                        }
                        finally
                        {
                            await EndProgressAsync("RTM: workspace indexing complete", cancellationToken).ConfigureAwait(false);
                        }

                        break;
                    }

                case "shutdown":
                    if (hasId) await SendResultAsync(id, null, cancellationToken).ConfigureAwait(false);
                    break;

                case "exit":
                    Environment.Exit(0);
                    break;

                case "textDocument/didOpen":
                    {
                        var args = paramsEl.Deserialize<DidOpenTextDocumentParams>(JsonOptions)!;
                        _documents.OpenOrUpdate(args.TextDocument.Uri, args.TextDocument.Text);
                        break;
                    }

                case "textDocument/didChange":
                    {
                        var args = paramsEl.Deserialize<DidChangeTextDocumentParams>(JsonOptions)!;
                        var text = args.ContentChanges.LastOrDefault()?.Text ?? string.Empty;
                        _documents.OpenOrUpdate(args.TextDocument.Uri, text);
                        break;
                    }

                case "textDocument/definition":
                    {
                        var args = paramsEl.Deserialize<TextDocumentPositionParams>(JsonOptions)!;
                        await SendResultAsync(id, _service.GetDefinition(args), cancellationToken).ConfigureAwait(false);
                        break;
                    }

                case "textDocument/references":
                    {
                        var args = paramsEl.Deserialize<ReferenceParams>(JsonOptions)!;
                        await SendResultAsync(id, _service.FindReferences(args), cancellationToken).ConfigureAwait(false);
                        break;
                    }

                case "textDocument/hover":
                    {
                        var args = paramsEl.Deserialize<TextDocumentPositionParams>(JsonOptions)!;
                        await SendResultAsync(id, _service.Hover(args), cancellationToken).ConfigureAwait(false);
                        break;
                    }

                default:
                    if (hasId)
                        await SendErrorAsync(id, -32601, $"Method not found: {method}", cancellationToken)
                            .ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception ex)
        {
            if (hasId) await SendErrorAsync(id, -32603, ex.Message, cancellationToken).ConfigureAwait(false);
        }
    }


    private void PublishStatus(string message, bool showToUser)
    {
        try
        {
            // window/logMessage appears in VS Code's language-server output.
            SendNotificationAsync("window/logMessage", new { type = 3, message }, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var token = _activeProgressToken;
            if (!string.IsNullOrWhiteSpace(token))
            {
                SendNotificationAsync("$/progress", new
                {
                    token,
                    value = new
                    {
                        kind = "report",
                        message
                    }
                }, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }

            // Keep popups for milestones/errors only. Per-file messages are shown in
            // the progress UI/output so the editor is not flooded with notifications.
            if (showToUser)
            {
                SendNotificationAsync("window/showMessage", new { type = 3, message }, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
        }
        catch
        {
            // Status feedback must never break LSP request handling.
        }
    }

    private async Task BeginProgressAsync(string title, CancellationToken cancellationToken)
    {
        var token = $"rtm-workspace-{Guid.NewGuid():N}";
        _activeProgressToken = token;

        await SendRequestAsync("window/workDoneProgress/create", new { token }, cancellationToken)
            .ConfigureAwait(false);

        await SendNotificationAsync("$/progress", new
        {
            token,
            value = new
            {
                kind = "begin",
                title,
                message = "Scanning RTM files",
                percentage = 0
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task EndProgressAsync(string message, CancellationToken cancellationToken)
    {
        var token = _activeProgressToken;
        if (string.IsNullOrWhiteSpace(token))
            return;

        await SendNotificationAsync("$/progress", new
        {
            token,
            value = new
            {
                kind = "end",
                message
            }
        }, cancellationToken).ConfigureAwait(false);

        _activeProgressToken = null;
    }

    private async Task<string?> ReadMessageAsync(CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            var line = await ReadAsciiLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) return null;
            if (line.Length == 0) break;
            var idx = line.IndexOf(':');
            if (idx > 0) headers[line[..idx].Trim()] = line[(idx + 1)..].Trim();
        }

        if (!headers.TryGetValue("Content-Length", out var lengthText) || !int.TryParse(lengthText, out var length))
            return null;

        var buffer = new byte[length];
        var read = 0;
        while (read < length)
        {
            var n = await _input.ReadAsync(buffer.AsMemory(read, length - read), cancellationToken)
                .ConfigureAwait(false);
            if (n == 0) return null;
            read += n;
        }

        return Encoding.UTF8.GetString(buffer);
    }

    private async Task<string?> ReadAsciiLineAsync(CancellationToken cancellationToken)
    {
        var bytes = new List<byte>();
        while (true)
        {
            var b = new byte[1];
            var n = await _input.ReadAsync(b.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
            if (n == 0) return bytes.Count == 0 ? null : Encoding.ASCII.GetString(bytes.ToArray()).TrimEnd('\r');
            if (b[0] == (byte)'\n') return Encoding.ASCII.GetString(bytes.ToArray()).TrimEnd('\r');
            bytes.Add(b[0]);
        }
    }

    private Task SendResultAsync(JsonElement id, object? result, CancellationToken cancellationToken) =>
        SendAsync(new { jsonrpc = "2.0", id, result }, cancellationToken);

    private Task SendErrorAsync(JsonElement id, int code, string message, CancellationToken cancellationToken) =>
        SendAsync(new { jsonrpc = "2.0", id, error = new { code, message } }, cancellationToken);

    private Task SendRequestAsync(string method, object? @params, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _nextRequestId);
        return SendAsync(new { jsonrpc = "2.0", id, method, @params }, cancellationToken);
    }

    private Task SendNotificationAsync(string method, object? @params, CancellationToken cancellationToken) =>
        SendAsync(new { jsonrpc = "2.0", method, @params }, cancellationToken);

    private async Task SendAsync(object message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var body = Encoding.UTF8.GetBytes(json);
        var header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");

        await _sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _output.WriteAsync(header, cancellationToken).ConfigureAwait(false);
            await _output.WriteAsync(body, cancellationToken).ConfigureAwait(false);
            await _output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendGate.Release();
        }
    }
}