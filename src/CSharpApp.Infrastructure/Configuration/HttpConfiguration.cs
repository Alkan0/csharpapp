namespace CSharpApp.Infrastructure.Configuration;

public static class HttpConfiguration
{
    public static IServiceCollection AddHttpConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var restApiSettings = configuration.GetSection(nameof(RestApiSettings)).Get<RestApiSettings>()
            ?? throw new InvalidOperationException($"{nameof(RestApiSettings)} configuration is missing.");
        var httpClientSettings = configuration.GetSection(nameof(HttpClientSettings)).Get<HttpClientSettings>()
            ?? new HttpClientSettings();

        if (string.IsNullOrWhiteSpace(restApiSettings.BaseUrl))
        {
            throw new InvalidOperationException($"{nameof(RestApiSettings.BaseUrl)} configuration is missing.");
        }

        if (httpClientSettings.LifeTime <= 0)
        {
            throw new InvalidOperationException($"{nameof(HttpClientSettings.LifeTime)} configuration must be greater than zero.");
        }

        if (httpClientSettings.RetryCount < 0)
        {
            throw new InvalidOperationException($"{nameof(HttpClientSettings.RetryCount)} configuration cannot be negative.");
        }

        if (httpClientSettings.RetryCount > 0 && httpClientSettings.SleepDuration <= 0)
        {
            throw new InvalidOperationException($"{nameof(HttpClientSettings.SleepDuration)} configuration must be greater than zero when retries are enabled.");
        }

        var handlerLifetime = TimeSpan.FromMinutes(httpClientSettings.LifeTime);
        var baseAddress = new Uri(restApiSettings.BaseUrl);
        var noRetryPolicy = Policy.NoOpAsync<HttpResponseMessage>();

        services.AddThirdPartyHttpClient<IProductsService, ProductsService>(
            baseAddress,
            handlerLifetime,
            httpClientSettings,
            noRetryPolicy);

        services.AddThirdPartyHttpClient<ICategoriesService, CategoriesService>(
            baseAddress,
            handlerLifetime,
            httpClientSettings,
            noRetryPolicy);

        services.AddThirdPartyHttpClient<IAuthService, AuthService>(
            baseAddress,
            handlerLifetime,
            httpClientSettings,
            noRetryPolicy);

        return services;
    }

    private static void AddThirdPartyHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        Uri baseAddress,
        TimeSpan handlerLifetime,
        HttpClientSettings settings,
        IAsyncPolicy<HttpResponseMessage> noRetryPolicy)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>(client =>
        {
            client.BaseAddress = baseAddress;
        })
        .SetHandlerLifetime(handlerLifetime)
        .AddPolicyHandler((serviceProvider, request) =>
        {
            if (!IsIdempotent(request.Method))
            {
                return noRetryPolicy;
            }

            var logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(TImplementation));

            return CreateRetryPolicy(settings, logger, request);
        });
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(
        HttpClientSettings settings,
        ILogger logger,
        HttpRequestMessage request)
    {
        if (settings.RetryCount == 0)
        {
            return Policy.NoOpAsync<HttpResponseMessage>();
        }

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                settings.RetryCount,
                retryAttempt => GetRetryDelay(settings.SleepDuration, retryAttempt),
                (outcome, delay, retryAttempt, _) => LogRetry(logger, request, outcome, delay, retryAttempt));
    }

    private static void LogRetry(
        ILogger logger,
        HttpRequestMessage request,
        DelegateResult<HttpResponseMessage> outcome,
        TimeSpan delay,
        int retryAttempt)
    {
        logger.LogWarning(
            outcome.Exception,
            "Retrying third-party HTTP request. Method: {Method}, Uri: {Uri}, RetryAttempt: {RetryAttempt}, DelayMilliseconds: {DelayMilliseconds}, StatusCode: {StatusCode}",
            request.Method,
            request.RequestUri,
            retryAttempt,
            delay.TotalMilliseconds,
            outcome.Result?.StatusCode);
    }

    private static TimeSpan GetRetryDelay(int sleepDuration, int retryAttempt)
    {
        var exponentialBackoff = Math.Pow(2, retryAttempt - 1);
        return TimeSpan.FromMilliseconds(sleepDuration * exponentialBackoff);
    }

    private static bool IsIdempotent(HttpMethod method)
    {
        return method == HttpMethod.Get
            || method == HttpMethod.Head
            || method == HttpMethod.Put
            || method == HttpMethod.Delete
            || method == HttpMethod.Options
            || method == HttpMethod.Trace;
    }
}
