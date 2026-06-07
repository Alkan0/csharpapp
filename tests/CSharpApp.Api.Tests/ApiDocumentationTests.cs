namespace CSharpApp.Api.Tests;

public sealed class ApiDocumentationTests(ApiTestApplicationFactory factory) : IClassFixture<ApiTestApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SwaggerDocument_ReturnsOk()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SwaggerDocument_UsesConcreteApiVersionInPaths()
    {
        var swaggerDocument = await _client.GetStringAsync("/swagger/v1/swagger.json");

        Assert.Contains("/api/v1/products", swaggerDocument);
        Assert.DoesNotContain("v{version}", swaggerDocument);
    }
}
