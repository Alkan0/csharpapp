using CSharpApp.Core.Dtos;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CSharpApp.Api.Tests;

public sealed class CategoriesEndpointsTests(ApiTestApplicationFactory factory) : IClassFixture<ApiTestApplicationFactory>
{
    private readonly HttpClient _client = CreateAuthenticatedClient(factory);

    [Fact]
    public async Task GetCategories_WithoutBearerToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/categories");

        response.EnsureSuccessStatusCode();
        var categories = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<Category>>();

        Assert.NotNull(categories);
        Assert.Single(categories);
    }

    [Fact]
    public async Task GetCategory_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/categories/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_ReturnsOk()
    {
        var request = new UpdateCategoryRequest
        {
            Name = "Updated category",
            Image = "https://example.com/category.png"
        };

        var response = await _client.PutAsJsonAsync("/api/v1/categories/1", request);

        response.EnsureSuccessStatusCode();
        var category = await response.Content.ReadFromJsonAsync<Category>();
        Assert.Equal("Updated category", category?.Name);
    }

    [Fact]
    public async Task UpdateCategory_WithPartialRequest_ReturnsOk()
    {
        var request = new UpdateCategoryRequest
        {
            Name = "Updated category"
        };

        var response = await _client.PutAsJsonAsync("/api/v1/categories/1", request);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateCategory_WithInvalidRequest_ReturnsBadRequest()
    {
        var request = new CreateCategoryRequest
        {
            Name = "",
            Image = ""
        };

        var response = await _client.PostAsJsonAsync("/api/v1/categories", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_WithInvalidRequest_ReturnsBadRequest()
    {
        var request = new UpdateCategoryRequest
        {
            Name = "",
            Image = ""
        };

        var response = await _client.PutAsJsonAsync("/api/v1/categories/1", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_ReturnsNoContent()
    {
        var response = await _client.DeleteAsync("/api/v1/categories/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/v1/categories/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCategoryProducts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/categories/1/products");

        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<Product>>();
        var product = Assert.Single(products!);
        Assert.Equal("Category product", product.Title);
    }

    private static HttpClient CreateAuthenticatedClient(ApiTestApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-access-token");
        return client;
    }
}
