using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RtmLanguageServer.Core.Parsing;

namespace RtmLanguageServer.Protocol;

public sealed class JsonRpcServer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Stream _input;
    private readonly Stream _output;
    private readonly DocumentStore _documents;
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private readonly RtmLanguageService _service;
    private readonly ILogger<JsonRpcServer> _logger;
    private DateTime _lastStatusBarUpdateUtc = DateTime.MinValue;
    private readonly ConcurrentQueue<Func<Task>> _backgroundNotifications = new();

    public JsonRpcServer(Stream input, Stream output, ILoggerFactory loggerFactory)
    {
        _input = input;
        _output = output;
        _logger = loggerFactory.CreateLogger<JsonRpcServer>();
        _documents = new DocumentStore(loggerFactory);
        _documents.Status += PublishStatus;
        _service = new RtmLanguageService(_documents);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var backgroundTask = Task.Run(() => ProcessBackgroundNotificationsAsync(cts.Token), cts.Token);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var payload = await ReadMessageAsync(cancellationToken).ConfigureAwait(false);
                if (payload is null) break;
                await HandleMessageAsync(payload, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            cts.Cancel(); // Signal background task to stop
            try
            {
                await backgroundTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
        }
    }

    private async Task ProcessBackgroundNotificationsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_backgroundNotifications.TryDequeue(out var notification))
                {
                    await notification().ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background notification failed");
            }
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
                            : [args.RootUri ?? args.RootPath ?? ""];

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

                        _backgroundNotifications.Enqueue(() => SendStatusBarNotificationAsync("RTM: Loading workspace", busy: true));

                        foreach (var workspaceRoot in roots)
                            _documents.LoadWorkspace(workspaceRoot);

                        _backgroundNotifications.Enqueue(() => SendStatusBarNotificationAsync("RTM: Ready", busy: false));

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
                        var document = _documents.OpenOrUpdate(args.TextDocument.Uri, args.TextDocument.Text);
                        PublishDiagnostics(document);
                        break;
                    }

                case "textDocument/didChange":
                    {
                        var args = paramsEl.Deserialize<DidChangeTextDocumentParams>(JsonOptions)!;
                        var text = args.ContentChanges.LastOrDefault()?.Text ?? string.Empty;
                        var document = _documents.OpenOrUpdate(args.TextDocument.Uri, text);
                        PublishDiagnostics(document);
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
                    else
                        _logger.LogWarning("Received unknown notification method: {Method}", method);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling LSP method {Method}", method);
            if (hasId) await SendErrorAsync(id, -32603, ex.Message, cancellationToken).ConfigureAwait(false);
        }
    }

    private void PublishStatus(WorkspaceStatus status)
    {
        _backgroundNotifications.Enqueue(async () =>
        {
            try
            {
                await SendNotificationAsync("window/logMessage", new { type = 3, message = status.Message }, CancellationToken.None)
                    .ConfigureAwait(false);

                if (ShouldSendStatusBarUpdate(status))
                {
                    await SendStatusBarNotificationAsync(status.Message, status.IsBusy).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish status message");
            }
        });
    }

    private bool ShouldSendStatusBarUpdate(WorkspaceStatus status)
    {
        if (status.Kind == StatusKind.Important)
            return true;

        if (status.Kind == StatusKind.Milestone)
        {
            var now = DateTime.UtcNow;
            if (now - _lastStatusBarUpdateUtc < TimeSpan.FromMilliseconds(250))
                return false;

            _lastStatusBarUpdateUtc = now;
            return true;
        }

        return false;
    }

    private Task SendStatusBarNotificationAsync(string message, bool busy)
    {
        return SendNotificationAsync("rtm/status", new
        {
            message,
            busy
        }, CancellationToken.None);
    }

    private void PublishDiagnostics(RtmDocument document)
    {
        _backgroundNotifications.Enqueue(async () =>
        {
            try
            {
                var diagnostics = document.Diagnostics
                    .Select(d => new Diagnostic
                    {
                        Range = d.Range,
                        Severity = (int)d.Severity,
                        Source = d.Source,
                        Message = d.Message
                    })
                    .ToList();

                var publishParams = new PublishDiagnosticsParams
                {
                    Uri = document.Uri,
                    Diagnostics = diagnostics
                };

                await SendNotificationAsync("textDocument/publishDiagnostics", publishParams, CancellationToken.None)
                    .ConfigureAwait(false);

                if (diagnostics.Count > 0)
                {
                    _logger.LogInformation("Published {Count} diagnostic(s) for {Uri}", diagnostics.Count, document.Uri);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish diagnostics for {Uri}", document.Uri);
            }
        });
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
