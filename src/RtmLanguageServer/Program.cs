using RtmLanguageServer.Lsp;

Console.Error.WriteLine("RTM SERVER STARTED");
var server = new JsonRpcServer(Console.OpenStandardInput(), Console.OpenStandardOutput());
await server.RunAsync();