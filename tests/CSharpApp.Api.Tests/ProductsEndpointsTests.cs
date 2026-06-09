using CSharpApp.Core.Dtos;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CSharpApp.Api.Tests;

public sealed class ProductsEndpointsTests(ApiTestApplicationFactory factory) : IClassFixture<ApiTestApplicationFactory>
{
    private readonly HttpClient _client = CreateAuthenticatedClient(factory);

    [Fact]
    public async Task GetProducts_WithoutBearerToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/products");

        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<Product>>();

        Assert.NotNull(products);
        Assert.Single(products);
    }

    [Fact]
    public async Task GetProduct_WhenProductDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreated()
    {
        var request = new CreateProductRequest
        {
            Title = "Created product",
            Price = 20,
            Description = "Created description",
            CategoryId = 1,
            Images = ["https://example.com/product.png"]
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        Assert.Equal("Created product", product?.Title);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidRequest_ReturnsBadRequest()
    {
        var request = new CreateProductRequest
        {
            Title = "",
            Price = 0,
            Description = "",
            CategoryId = 0,
            Images = []
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static HttpClient CreateAuthenticatedClient(ApiTestApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-access-token");
        return client;
    }
}
