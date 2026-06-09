namespace CSharpApp.Api.Tests;

using System.Text.Json;

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

    [Fact]
    public async Task SwaggerDocument_DefinesBearerAuthentication()
    {
        var swaggerDocument = await _client.GetStringAsync("/swagger/v1/swagger.json");
        using var document = JsonDocument.Parse(swaggerDocument);

        var securityScheme = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("ThirdPartyBearer");

        Assert.Equal("http", securityScheme.GetProperty("type").GetString());
        Assert.Equal("bearer", securityScheme.GetProperty("scheme").GetString());
        Assert.Equal("JWT", securityScheme.GetProperty("bearerFormat").GetString());
    }
}
