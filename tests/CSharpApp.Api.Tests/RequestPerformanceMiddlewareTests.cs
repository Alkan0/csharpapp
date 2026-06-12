using CSharpApp.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

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
        context.Request.Host = new HostString("localhost:5225");
        context.Request.RouteValues["version"] = "1";
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "HTTP: GET /api/v{version:apiVersion}/products"));
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "123")
        ], "Test"));

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
        Assert.Contains("completed", entry.Message);
        Assert.Equal("completed", entry.Properties["EventName"]);
        Assert.Equal(204, entry.Properties["StatusCode"]);
        Assert.Equal("HTTP: GET /api/v1/products", entry.Properties["EndpointName"]);
        Assert.Equal(true, entry.Properties["IsAuthenticated"]);
        Assert.Equal("123", entry.Properties["UserId"]);
        Assert.Equal("localhost:5225", entry.Properties["RequestHost"]);
        Assert.Equal("IncomingRequest", entry.Properties["LogType"]);
        Assert.Equal(context.TraceIdentifier, entry.Properties["RequestId"]);
        Assert.Equal(context.TraceIdentifier, entry.Properties["CorrelationId"]);
        Assert.Equal(context.TraceIdentifier, context.Response.Headers["X-Correlation-ID"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenClientErrorOccurs_LogsInformation()
    {
        var logger = new TestLogger<RequestPerformanceMiddleware>();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/auth/profile";
        context.Request.Host = new HostString("localhost:5225");

        var middleware = new RequestPerformanceMiddleware(
            next: httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Equal("completed", entry.Properties["EventName"]);
        Assert.Equal(401, entry.Properties["StatusCode"]);
        Assert.Equal("IncomingRequest", entry.Properties["LogType"]);
    }

    [Fact]
    public async Task InvokeAsync_WhenCorrelationHeaderExists_UsesItInLogAndResponse()
    {
        var logger = new TestLogger<RequestPerformanceMiddleware>();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/products";
        context.Request.Host = new HostString("localhost:5225");
        context.Request.Headers["X-Correlation-ID"] = "external-correlation-id";

        var middleware = new RequestPerformanceMiddleware(
            next: httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal("external-correlation-id", entry.Properties["CorrelationId"]);
        Assert.Equal("external-correlation-id", context.Response.Headers["X-Correlation-ID"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenRouteHasConstrainedId_ReplacesIdInEndpointName()
    {
        var logger = new TestLogger<RequestPerformanceMiddleware>();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/products/42";
        context.Request.Host = new HostString("localhost:5225");
        context.Request.RouteValues["version"] = "1";
        context.Request.RouteValues["id"] = "42";
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "HTTP: GET /api/v{version:apiVersion}/products/{id:int}"));

        var middleware = new RequestPerformanceMiddleware(
            next: httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal("HTTP: GET /api/v1/products/42", entry.Properties["EndpointName"]);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestFails_LogsFailureAndRethrows()
    {
        var logger = new TestLogger<RequestPerformanceMiddleware>();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/products";
        context.Request.Host = new HostString("localhost:5225");
        context.Request.RouteValues["version"] = "1";
        var expectedException = new InvalidOperationException("Third-party request failed.");

        var middleware = new RequestPerformanceMiddleware(
            next: _ => throw expectedException,
            logger);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.Same(expectedException, exception);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.LogLevel);
        Assert.Same(expectedException, entry.Exception);
        Assert.Contains("failed", entry.Message);
        Assert.Equal("failed", entry.Properties["EventName"]);
        Assert.Equal(500, entry.Properties["StatusCode"]);
        Assert.Equal(false, entry.Properties["IsAuthenticated"]);
        Assert.Equal("localhost:5225", entry.Properties["RequestHost"]);
        Assert.Equal("IncomingRequest", entry.Properties["LogType"]);
        Assert.Equal(context.TraceIdentifier, entry.Properties["RequestId"]);
        Assert.Equal(context.TraceIdentifier, entry.Properties["CorrelationId"]);
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
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception, GetProperties(state)));
        }

        private static IReadOnlyDictionary<string, object?> GetProperties<TState>(TState state)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> properties)
            {
                return new Dictionary<string, object?>();
            }

            return properties
                .Where(property => property.Key != "{OriginalFormat}")
                .ToDictionary(property => property.Key, property => property.Value);
        }
    }

    private sealed record LogEntry(
        LogLevel LogLevel,
        string Message,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> Properties);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
