namespace CSharpApp.Application.Tests.Products;

public sealed class ProductHandlersTests
{
    [Fact]
    public async Task GetProductsQueryHandler_ReturnsProductsFromService()
    {
        var service = new FakeProductsService();
        var handler = new GetProductsQueryHandler(service);

        var products = await handler.Handle(new GetProductsQuery(), CancellationToken.None);

        var product = Assert.Single(products);
        Assert.Equal("Product 1", product.Title);
        Assert.Equal(1, service.GetProductsCallCount);
    }

    [Fact]
    public async Task GetProductQueryHandler_ReturnsProductFromService()
    {
        var service = new FakeProductsService();
        var handler = new GetProductQueryHandler(service);

        var product = await handler.Handle(new GetProductQuery(1), CancellationToken.None);

        Assert.NotNull(product);
        Assert.Equal(1, product.Id);
        Assert.Equal(1, service.LastProductId);
    }

    [Fact]
    public async Task CreateProductCommandHandler_ReturnsCreatedProductFromService()
    {
        var service = new FakeProductsService();
        var handler = new CreateProductCommandHandler(service);
        var request = new CreateProductRequest
        {
            Title = "Created product",
            Price = 20,
            Description = "Description",
            CategoryId = 1,
            Images = ["https://example.com/product.png"]
        };

        var product = await handler.Handle(new CreateProductCommand(request), CancellationToken.None);

        Assert.NotNull(product);
        Assert.Equal("Created product", product.Title);
        Assert.Same(request, service.LastCreateRequest);
    }

    private sealed class FakeProductsService : IProductsService
    {
        public int GetProductsCallCount { get; private set; }
        public int? LastProductId { get; private set; }
        public CreateProductRequest? LastCreateRequest { get; private set; }

        public Task<IReadOnlyCollection<Product>> GetProducts()
        {
            GetProductsCallCount++;

            IReadOnlyCollection<Product> products =
            [
                new Product
                {
                    Id = 1,
                    Title = "Product 1"
                }
            ];

            return Task.FromResult(products);
        }

        public Task<Product?> GetProduct(int id)
        {
            LastProductId = id;

            return Task.FromResult<Product?>(new Product
            {
                Id = id,
                Title = $"Product {id}"
            });
        }

        public Task<Product?> CreateProduct(CreateProductRequest request)
        {
            LastCreateRequest = request;

            return Task.FromResult<Product?>(new Product
            {
                Id = 100,
                Title = request.Title
            });
        }
    }
}
