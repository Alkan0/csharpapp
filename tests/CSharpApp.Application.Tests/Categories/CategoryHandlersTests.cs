namespace CSharpApp.Application.Tests.Categories;

public sealed class CategoryHandlersTests
{
    [Fact]
    public async Task GetCategoriesQueryHandler_ReturnsCategoriesFromService()
    {
        var service = new FakeCategoriesService();
        var handler = new GetCategoriesQueryHandler(service);

        var categories = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        var category = Assert.Single(categories);
        Assert.Equal("Category 1", category.Name);
        Assert.Equal(1, service.GetCategoriesCallCount);
    }

    [Fact]
    public async Task GetCategoryQueryHandler_ReturnsCategoryFromService()
    {
        var service = new FakeCategoriesService();
        var handler = new GetCategoryQueryHandler(service);

        var category = await handler.Handle(new GetCategoryQuery(1), CancellationToken.None);

        Assert.NotNull(category);
        Assert.Equal(1, category.Id);
        Assert.Equal(1, service.LastCategoryId);
    }

    [Fact]
    public async Task CreateCategoryCommandHandler_ReturnsCreatedCategoryFromService()
    {
        var service = new FakeCategoriesService();
        var handler = new CreateCategoryCommandHandler(service);
        var request = new CreateCategoryRequest
        {
            Name = "Created category",
            Image = "https://example.com/category.png"
        };

        var category = await handler.Handle(new CreateCategoryCommand(request), CancellationToken.None);

        Assert.NotNull(category);
        Assert.Equal("Created category", category.Name);
        Assert.Same(request, service.LastCreateRequest);
    }

    [Fact]
    public async Task UpdateCategoryCommandHandler_ReturnsUpdatedCategoryFromService()
    {
        var service = new FakeCategoriesService();
        var handler = new UpdateCategoryCommandHandler(service);
        var request = new UpdateCategoryRequest
        {
            Name = "Updated category",
            Image = "https://example.com/category.png"
        };

        var category = await handler.Handle(new UpdateCategoryCommand(1, request), CancellationToken.None);

        Assert.NotNull(category);
        Assert.Equal("Updated category", category.Name);
        Assert.Equal(1, service.LastCategoryId);
        Assert.Same(request, service.LastUpdateRequest);
    }

    [Fact]
    public async Task DeleteCategoryCommandHandler_ReturnsDeleteResultFromService()
    {
        var service = new FakeCategoriesService();
        var handler = new DeleteCategoryCommandHandler(service);

        var deleted = await handler.Handle(new DeleteCategoryCommand(1), CancellationToken.None);

        Assert.True(deleted);
        Assert.Equal(1, service.LastCategoryId);
    }

    [Fact]
    public async Task GetCategoryProductsQueryHandler_ReturnsProductsFromService()
    {
        var service = new FakeCategoriesService();
        var handler = new GetCategoryProductsQueryHandler(service);

        var products = await handler.Handle(new GetCategoryProductsQuery(1), CancellationToken.None);

        var product = Assert.Single(products);
        Assert.Equal("Category product", product.Title);
        Assert.Equal(1, service.LastCategoryId);
    }

    private sealed class FakeCategoriesService : ICategoriesService
    {
        public int GetCategoriesCallCount { get; private set; }
        public int? LastCategoryId { get; private set; }
        public CreateCategoryRequest? LastCreateRequest { get; private set; }
        public UpdateCategoryRequest? LastUpdateRequest { get; private set; }

        public Task<IReadOnlyCollection<Category>> GetCategories()
        {
            GetCategoriesCallCount++;

            IReadOnlyCollection<Category> categories =
            [
                new Category
                {
                    Id = 1,
                    Name = "Category 1"
                }
            ];

            return Task.FromResult(categories);
        }

        public Task<Category?> GetCategory(int id)
        {
            LastCategoryId = id;

            return Task.FromResult<Category?>(new Category
            {
                Id = id,
                Name = $"Category {id}"
            });
        }

        public Task<Category?> CreateCategory(CreateCategoryRequest request)
        {
            LastCreateRequest = request;

            return Task.FromResult<Category?>(new Category
            {
                Id = 100,
                Name = request.Name,
                Image = request.Image
            });
        }

        public Task<Category?> UpdateCategory(int id, UpdateCategoryRequest request)
        {
            LastCategoryId = id;
            LastUpdateRequest = request;

            return Task.FromResult<Category?>(new Category
            {
                Id = id,
                Name = request.Name,
                Image = request.Image
            });
        }

        public Task<bool> DeleteCategory(int id)
        {
            LastCategoryId = id;

            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<Product>> GetCategoryProducts(int id)
        {
            LastCategoryId = id;

            IReadOnlyCollection<Product> products =
            [
                new Product
                {
                    Id = 1,
                    Title = "Category product",
                    Category = new Category
                    {
                        Id = id,
                        Name = $"Category {id}"
                    }
                }
            ];

            return Task.FromResult(products);
        }
    }
}
