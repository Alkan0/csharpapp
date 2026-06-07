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

    [Fact]
    public async Task DeleteCategory_ReturnsTrue_WhenApiReturnsTrue()
    {
        var service = CreateService(_ => TestHttpMessageHandler.Json(HttpStatusCode.OK, "true"));

        var deleted = await service.DeleteCategory(1);

        Assert.True(deleted);
    }

    [Fact]
    public async Task DeleteCategory_ReturnsFalse_WhenApiReturnsBadRequest()
    {
        var service = CreateService(_ => TestHttpMessageHandler.Json(HttpStatusCode.BadRequest, "{}"));

        var deleted = await service.DeleteCategory(999);

        Assert.False(deleted);
    }

    [Fact]
    public async Task GetCategoryProducts_ReturnsProducts_WhenApiReturnsValidResponse()
    {
        var service = CreateService(_ => TestHttpMessageHandler.Json(HttpStatusCode.OK, """
            [
              {
                "id": 1,
                "title": "Product 1",
                "slug": "product-1",
                "price": 10,
                "description": "Description",
                "images": [],
                "category": { "id": 2, "name": "Category", "slug": "category", "image": "https://example.com/image.jpg" }
              }
            ]
            """));

        var products = await service.GetCategoryProducts(1);

        var product = Assert.Single(products);
        Assert.Equal("Product 1", product.Title);
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
