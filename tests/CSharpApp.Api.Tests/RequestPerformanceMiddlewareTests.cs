using CSharpApp.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CSharpApp.Api.Tests;

public sealed class RequestPerformanceMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenRequestSucceeds_LogsResponseStatus()
    {
        var logger = new TestLogger<RequestPerformanceMiddleware>();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/products";

        var middleware = new RequestPerformanceMiddleware(
            next: httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains("responded 204", entry.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestFails_LogsFailureAndRethrows()
    {
        var logger = new TestLogger<RequestPerformanceMiddleware>();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/products";
        var expectedException = new InvalidOperationException("Third-party request failed.");

        var middleware = new RequestPerformanceMiddleware(
            next: _ => throw expectedException,
            logger);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.Same(expectedException, exception);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.LogLevel);
        Assert.Same(expectedException, entry.Exception);
        Assert.Contains("failed 500", entry.Message);
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
