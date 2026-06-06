using CSharpApp.Core.Dtos;
using System.Net;
using System.Net.Http.Json;

namespace CSharpApp.Api.Tests;

public sealed class CategoriesEndpointsTests(ApiTestApplicationFactory factory) : IClassFixture<ApiTestApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

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
}
