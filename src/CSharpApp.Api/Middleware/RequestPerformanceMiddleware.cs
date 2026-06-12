using System.Diagnostics;

namespace CSharpApp.Api.Middleware;

public class RequestPerformanceMiddleware
{
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
        try
        {
            await _next(context);
            stopwatch.Stop();
            _logger.LogInformation(
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds} ms. Endpoint: {EndpointName}, Authenticated: {IsAuthenticated}, UserId: {UserId}, Host: {RequestHost}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                GetEndpointName(context),
                IsAuthenticated(context),
                GetUserId(context),
                context.Request.Host.Value);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "HTTP {RequestMethod} {RequestPath} failed {StatusCode} in {ElapsedMilliseconds} ms. Endpoint: {EndpointName}, Authenticated: {IsAuthenticated}, UserId: {UserId}, Host: {RequestHost}",
                context.Request.Method,
                context.Request.Path,
                StatusCodes.Status500InternalServerError,
                stopwatch.ElapsedMilliseconds,
                GetEndpointName(context),
                IsAuthenticated(context),
                GetUserId(context),
                context.Request.Host.Value);

            throw;
        }
    }

    private static string? GetEndpointName(HttpContext context)
    {
        var endpointName = context.GetEndpoint()?.DisplayName;
        var apiVersion = GetApiVersion(context);

        return string.IsNullOrWhiteSpace(apiVersion)
            ? endpointName
            : endpointName?.Replace("{version:apiVersion}", apiVersion, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetApiVersion(HttpContext context)
    {
        return context.Request.RouteValues["version"]?.ToString();
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
