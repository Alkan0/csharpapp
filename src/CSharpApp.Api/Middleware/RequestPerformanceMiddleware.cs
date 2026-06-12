using System.Diagnostics;

namespace CSharpApp.Api.Middleware;

public class RequestPerformanceMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string IncomingRequestLogType = "IncomingRequest";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestPerformanceMiddleware> _logger;

    public RequestPerformanceMiddleware(
        RequestDelegate next,
        ILogger<RequestPerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;
        var correlationId = GetOrCreateCorrelationId(context);

        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        using (_logger.BeginScope(CreateRequestScope(context, correlationId)))
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                LogRequest(context, stopwatch.ElapsedMilliseconds, correlationId, exception);
            }
        }
    }

    private void LogRequest(
        HttpContext context,
        long elapsedMilliseconds,
        string correlationId,
        Exception? exception)
    {
        var statusCode = exception is null
            ? context.Response.StatusCode
            : StatusCodes.Status500InternalServerError;
        var logLevel = GetLogLevel(statusCode, exception);
        var eventName = exception is null ? "completed" : "failed";

        _logger.Log(
            logLevel,
            exception,
            "Incoming HTTP request {EventName}. Method: {RequestMethod}, Path: {RequestPath}, StatusCode: {StatusCode}, ElapsedMilliseconds: {ElapsedMilliseconds}, Endpoint: {EndpointName}, Authenticated: {IsAuthenticated}, UserId: {UserId}, Host: {RequestHost}, CorrelationId: {CorrelationId}, RequestId: {RequestId}, DistributedTraceId: {DistributedTraceId}, SpanId: {DistributedSpanId}, LogType: {LogType}",
            eventName,
            context.Request.Method,
            context.Request.Path,
            statusCode,
            elapsedMilliseconds,
            GetEndpointName(context),
            IsAuthenticated(context),
            GetUserId(context),
            context.Request.Host.Value,
            correlationId,
            context.TraceIdentifier,
            Activity.Current?.TraceId.ToString(),
            Activity.Current?.SpanId.ToString(),
            IncomingRequestLogType);
    }

    private static LogLevel GetLogLevel(int statusCode, Exception? exception)
    {
        if (exception is not null || statusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogLevel.Error;
        }

        return LogLevel.Information;
    }

    private static string? GetEndpointName(HttpContext context)
    {
        var endpointName = context.GetEndpoint()?.DisplayName;
        if (string.IsNullOrWhiteSpace(endpointName))
        {
            return endpointName;
        }

        foreach (var routeValue in context.Request.RouteValues)
        {
            var value = routeValue.Value?.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            endpointName = ReplaceRouteValue(endpointName, routeValue.Key, value);
        }

        return endpointName;
    }

    private static string ReplaceRouteValue(string endpointName, string routeKey, string routeValue)
    {
        var constrainedParameterPattern = $@"\{{{System.Text.RegularExpressions.Regex.Escape(routeKey)}:[^}}]+}}";

        return System.Text.RegularExpressions.Regex.Replace(
            endpointName.Replace($"{{{routeKey}}}", routeValue, StringComparison.OrdinalIgnoreCase),
            constrainedParameterPattern,
            routeValue,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].ToString();

        return string.IsNullOrWhiteSpace(correlationId)
            ? context.TraceIdentifier
            : correlationId;
    }

    private static Dictionary<string, object?> CreateRequestScope(HttpContext context, string correlationId)
    {
        return new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["RequestId"] = context.TraceIdentifier,
            ["RequestPath"] = context.Request.Path.Value,
            ["RequestMethod"] = context.Request.Method,
            ["LogScope"] = "HttpRequest"
        };
    }

    private static bool IsAuthenticated(HttpContext context)
    {
        return context.User.Identity?.IsAuthenticated == true;
    }

    private static string? GetUserId(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
