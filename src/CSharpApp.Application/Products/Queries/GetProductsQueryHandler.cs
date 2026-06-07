namespace CSharpApp.Application.Products.Queries;

public sealed class GetProductsQueryHandler(IProductsService productsService)
    : IRequestHandler<GetProductsQuery, IReadOnlyCollection<Product>>
{
    public async Task<IReadOnlyCollection<Product>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        return await productsService.GetProducts();
    }
}
