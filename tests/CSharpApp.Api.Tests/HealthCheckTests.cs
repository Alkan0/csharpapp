namespace CSharpApp.Api.Tests;

public sealed class HealthCheckTests(ApiTestApplicationFactory factory) : IClassFixture<ApiTestApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}
