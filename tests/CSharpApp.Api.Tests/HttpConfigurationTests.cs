using CSharpApp.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpApp.Api.Tests;

public sealed class HttpConfigurationTests
{
    [Fact]
    public void AddHttpConfiguration_WithRetrySettings_RegistersHttpClients()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(retryCount: 2, sleepDuration: 100);

        services.AddHttpConfiguration(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IHttpClientFactory>());
    }

    [Fact]
    public void AddHttpConfiguration_WithRetryCountAndMissingSleepDuration_Throws()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(retryCount: 2, sleepDuration: 0);

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddHttpConfiguration(configuration));

        Assert.Contains("SleepDuration", exception.Message);
    }

    [Fact]
    public void AddHttpConfiguration_WithNegativeRetryCount_Throws()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(retryCount: -1, sleepDuration: 100);

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddHttpConfiguration(configuration));

        Assert.Contains("RetryCount", exception.Message);
    }

    private static IConfiguration CreateConfiguration(int retryCount, int sleepDuration)
    {
        var values = new Dictionary<string, string?>
        {
            ["RestApiSettings:BaseUrl"] = "https://example.com/api/v1/",
            ["HttpClientSettings:LifeTime"] = "10",
            ["HttpClientSettings:RetryCount"] = retryCount.ToString(),
            ["HttpClientSettings:SleepDuration"] = sleepDuration.ToString()
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
