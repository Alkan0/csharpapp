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

        var handlerLifetime = TimeSpan.FromMinutes(httpClientSettings.LifeTime);

        services.AddHttpClient<IProductsService, ProductsService>(client =>
        {
            client.BaseAddress = new Uri(restApiSettings.BaseUrl);
        })
        .SetHandlerLifetime(handlerLifetime);

        services.AddHttpClient<ICategoriesService, CategoriesService>(client =>
        {
            client.BaseAddress = new Uri(restApiSettings.BaseUrl);
        })
        .SetHandlerLifetime(handlerLifetime);

        services.AddHttpClient<IAuthService, AuthService>(client =>
        {
            client.BaseAddress = new Uri(restApiSettings.BaseUrl);
        })
        .SetHandlerLifetime(handlerLifetime);

        return services;
    }
}
