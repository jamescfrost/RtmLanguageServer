using System.Text;
using Microsoft.Extensions.Logging;
using Moq;

namespace RtmLanguageServer.Protocol.UnitTests;

public class JsonRpcServerTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var input = new MemoryStream();
        var output = new MemoryStream();
        var loggerFactory = Mock.Of<ILoggerFactory>(lf =>
            lf.CreateLogger(It.IsAny<string>()) == Mock.Of<ILogger>());

        // Act
        var server = new JsonRpcServer(input, output, loggerFactory);

        // Assert
        Assert.NotNull(server);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesLoggerForJsonRpcServer()
    {
        // Arrange
        var input = new MemoryStream();
        var output = new MemoryStream();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger<JsonRpcServer>>();
        mockLoggerFactory
            .Setup(lf => lf.CreateLogger(It.IsAny<string>()))
            .Returns(mockLogger.Object);

        // Act
        var server = new JsonRpcServer(input, output, mockLoggerFactory.Object);

        // Assert
        mockLoggerFactory.Verify(lf => lf.CreateLogger(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RunAsync_WithEmptyStream_CompletesImmediately()
    {
        // Arrange
        var input = new MemoryStream();
        var output = new MemoryStream();
        var loggerFactory = Mock.Of<ILoggerFactory>(lf =>
            lf.CreateLogger(It.IsAny<string>()) == Mock.Of<ILogger>());
        var server = new JsonRpcServer(input, output, loggerFactory);

        // Act
        await server.RunAsync();

        // Assert - completes without exception
    }

    [Fact]
    public async Task RunAsync_WithCancellationToken_StopsWhenCancelled()
    {
        // Arrange
        var input = new MemoryStream();
        var output = new MemoryStream();
        var loggerFactory = Mock.Of<ILoggerFactory>(lf =>
            lf.CreateLogger(It.IsAny<string>()) == Mock.Of<ILogger>());
        var server = new JsonRpcServer(input, output, loggerFactory);
        var cts = new CancellationToken(true);

        // Act
        await server.RunAsync(cts);

        // Assert - completes without exception
    }

    [Fact]
    public async Task RunAsync_WithCancellationAfterStart_StopsGracefully()
    {
        // Arrange
        var input = new MemoryStream();
        var output = new MemoryStream();
        var loggerFactory = Mock.Of<ILoggerFactory>(lf =>
            lf.CreateLogger(It.IsAny<string>()) == Mock.Of<ILogger>());
        var server = new JsonRpcServer(input, output, loggerFactory);
        var cts = new CancellationTokenSource();

        // Act
        cts.CancelAfter(50);
        await server.RunAsync(cts.Token);

        // Assert - completes without exception
    }

    [Fact]
    public async Task RunAsync_WithValidJsonRpcMessage_ProcessesMessage()
    {
        // Arrange
        var initializeMessage = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{}}";
        var content = $"Content-Length: {Encoding.UTF8.GetByteCount(initializeMessage)}\r\n\r\n{initializeMessage}";
        var inputBytes = Encoding.UTF8.GetBytes(content);
        var input = new MemoryStream(inputBytes);
        var output = new MemoryStream();
        var loggerFactory = Mock.Of<ILoggerFactory>(lf =>
            lf.CreateLogger(It.IsAny<string>()) == Mock.Of<ILogger>());
        var server = new JsonRpcServer(input, output, loggerFactory);

        // Act
        await server.RunAsync();

        // Assert - completes without exception and processes the message
        Assert.True(output.Length > 0);
    }

    [Fact]
    public async Task RunAsync_WithMultipleMessages_ProcessesAllMessages()
    {
        // Arrange
        var message1 = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{}}";
        var message2 = "{\"jsonrpc\":\"2.0\",\"method\":\"initialized\",\"params\":{}}";
        var content1 = $"Content-Length: {Encoding.UTF8.GetByteCount(message1)}\r\n\r\n{message1}";
        var content2 = $"Content-Length: {Encoding.UTF8.GetByteCount(message2)}\r\n\r\n{message2}";
        var fullContent = content1 + content2;
        var inputBytes = Encoding.UTF8.GetBytes(fullContent);
        var input = new MemoryStream(inputBytes);
        var output = new MemoryStream();
        var loggerFactory = Mock.Of<ILoggerFactory>(lf =>
            lf.CreateLogger(It.IsAny<string>()) == Mock.Of<ILogger>());
        var server = new JsonRpcServer(input, output, loggerFactory);

        // Act
        await server.RunAsync();

        // Assert - completes without exception
    }

    [Fact]
    public async Task RunAsync_DefaultCancellationToken_AcceptsDefault()
    {
        // Arrange
        var input = new MemoryStream();
        var output = new MemoryStream();
        var loggerFactory = Mock.Of<ILoggerFactory>(lf =>
            lf.CreateLogger(It.IsAny<string>()) == Mock.Of<ILogger>());
        var server = new JsonRpcServer(input, output, loggerFactory);

        // Act
        await server.RunAsync(); // Uses default CancellationToken

        // Assert - completes without exception
    }

    [Fact]
    public async Task Initialize_ReturnsServerCapabilities()
    {
        // Arrange
        var initializeMessage = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{}}";
        var content = $"Content-Length: {Encoding.UTF8.GetByteCount(initializeMessage)}\r\n\r\n{initializeMessage}";
        var inputBytes = Encoding.UTF8.GetBytes(content);
        var input = new MemoryStream(inputBytes);
        var output = new MemoryStream();
        var loggerFactory = Mock.Of<ILoggerFactory>(lf =>
            lf.CreateLogger(It.IsAny<string>()) == Mock.Of<ILogger>());
        var server = new JsonRpcServer(input, output, loggerFactory);

        // Act
        await server.RunAsync();

        // Assert
        output.Seek(0, SeekOrigin.Begin);
        var responseText = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("capabilities", responseText);
        Assert.Contains("textDocumentSync", responseText);
        Assert.Contains("definitionProvider", responseText);
    }

    [Fact]
    public async Task Shutdown_ReturnsNullResult()
    {
        // Arrange
        var shutdownMessage = "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"shutdown\"}";
        var content = $"Content-Length: {Encoding.UTF8.GetByteCount(shutdownMessage)}\r\n\r\n{shutdownMessage}";
        var inputBytes = Encoding.UTF8.GetBytes(content);
        var input = new MemoryStream(inputBytes);
        var output = new MemoryStream();
        var loggerFactory = Mock.Of<ILoggerFactory>(lf =>
            lf.CreateLogger(It.IsAny<string>()) == Mock.Of<ILogger>());
        var server = new JsonRpcServer(input, output, loggerFactory);

        // Act
        await server.RunAsync();

        // Assert
        output.Seek(0, SeekOrigin.Begin);
        var responseText = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("\"result\":null", responseText);
    }

    [Fact]
    public async Task UnknownMethod_ReturnsMethodNotFoundError()
    {
        // Arrange
        var unknownMessage = "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"unknown/method\"}";
        var content = $"Content-Length: {Encoding.UTF8.GetByteCount(unknownMessage)}\r\n\r\n{unknownMessage}";
        var inputBytes = Encoding.UTF8.GetBytes(content);
        var input = new MemoryStream(inputBytes);
        var output = new MemoryStream();
        var loggerFactory = Mock.Of<ILoggerFactory>(lf =>
            lf.CreateLogger(It.IsAny<string>()) == Mock.Of<ILogger>());
        var server = new JsonRpcServer(input, output, loggerFactory);

        // Act
        await server.RunAsync();

        // Assert
        output.Seek(0, SeekOrigin.Begin);
        var responseText = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("error", responseText);
        Assert.Contains("-32601", responseText); // Method not found
    }
}
