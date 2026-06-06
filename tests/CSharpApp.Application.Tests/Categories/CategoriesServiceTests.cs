namespace CSharpApp.Application.Tests.Categories;

public class CategoriesServiceTests
{
    [Fact]
    public async Task GetCategories_ReturnsCategories_WhenApiReturnsValidResponse()
    {
        var service = CreateService(_ => TestHttpMessageHandler.Json(HttpStatusCode.OK, """
            [
              {
                "id": 1,
                "name": "Category 1",
                "image": "https://example.com/category.jpg"
              }
            ]
            """));

        var categories = await service.GetCategories();

        var category = Assert.Single(categories);
        Assert.Equal(1, category.Id);
        Assert.Equal("Category 1", category.Name);
    }

    [Fact]
    public async Task GetCategory_ReturnsNull_WhenApiReturnsBadRequest()
    {
        var service = CreateService(_ => TestHttpMessageHandler.Json(HttpStatusCode.BadRequest, "{}"));

        var category = await service.GetCategory(999);

        Assert.Null(category);
    }

    [Fact]
    public async Task CreateCategory_ThrowsHttpRequestException_WhenApiReturnsBadRequest()
    {
        var service = CreateService(_ => TestHttpMessageHandler.Json(HttpStatusCode.BadRequest, """
            { "message": "Invalid category" }
            """));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.CreateCategory(new CreateCategoryRequest
            {
                Name = "New Category",
                Image = "https://example.com/category.jpg"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_ReturnsNull_WhenApiReturnsBadRequest()
    {
        var service = CreateService(_ => TestHttpMessageHandler.Json(HttpStatusCode.BadRequest, """
            { "message": "Category not found" }
            """));

        var category = await service.UpdateCategory(999, new UpdateCategoryRequest
        {
            Name = "Updated Category",
            Image = "https://example.com/category.jpg"
        });

        Assert.Null(category);
    }

    private static CategoriesService CreateService(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var httpClient = new HttpClient(new TestHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://example.com/api/v1/")
        };

        var settings = Options.Create(new RestApiSettings
        {
            Categories = "categories"
        });

        return new CategoriesService(httpClient, settings, NullLogger<CategoriesService>.Instance);
    }
}
