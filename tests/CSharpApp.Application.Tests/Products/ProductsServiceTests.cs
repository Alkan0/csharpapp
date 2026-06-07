namespace CSharpApp.Application.Tests.Products;

public class ProductsServiceTests
{
    [Fact]
    public async Task GetProducts_ReturnsProducts_WhenApiReturnsValidResponse()
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

        var products = await service.GetProducts();

        var product = Assert.Single(products);
        Assert.Equal(1, product.Id);
        Assert.Equal("Product 1", product.Title);
        Assert.Equal("product-1", product.Slug);
        Assert.Equal("category", product.Category?.Slug);
    }

    [Fact]
    public async Task GetProduct_ReturnsNull_WhenApiReturnsBadRequest()
    {
        var service = CreateService(_ => TestHttpMessageHandler.Json(HttpStatusCode.BadRequest, "{}"));

        var product = await service.GetProduct(999);

        Assert.Null(product);
    }

    [Fact]
    public async Task CreateProduct_ThrowsHttpRequestException_WhenApiReturnsBadRequest()
    {
        var service = CreateService(_ => TestHttpMessageHandler.Json(HttpStatusCode.BadRequest, """
            { "message": "Category not found" }
            """));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.CreateProduct(new CreateProductRequest
            {
                Title = "New Product",
                Price = 10,
                Description = "Description",
                CategoryId = 999,
                Images = ["https://example.com/image.jpg"]
            }));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    private static ProductsService CreateService(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var httpClient = new HttpClient(new TestHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://example.com/api/v1/")
        };

        var settings = Options.Create(new RestApiSettings
        {
            Products = "products"
        });

        return new ProductsService(httpClient, settings, NullLogger<ProductsService>.Instance);
    }
}
