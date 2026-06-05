namespace CSharpApp.Infrastructure.Configuration;

public static class HttpConfiguration
{
    public static IServiceCollection AddHttpConfiguration(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetService<IConfiguration>();
        var restApiSettings = configuration!.GetSection(nameof(RestApiSettings)).Get<RestApiSettings>();

        services.AddHttpClient<IProductsService, ProductsService>(client =>
        {
            client.BaseAddress = new Uri(restApiSettings!.BaseUrl!);
        });

        return services;
    }
}