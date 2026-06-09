using Microsoft.Extensions.Logging;
using RtmLanguageServer.Protocol;

// Configure logging to stderr (LSP protocol uses stdout for communication)
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<Program>();
logger.LogInformation("RTM Language Server starting");

var server = new JsonRpcServer(
    Console.OpenStandardInput(),
    Console.OpenStandardOutput(),
    loggerFactory);

await server.RunAsync();

logger.LogInformation("RTM Language Server stopped");
