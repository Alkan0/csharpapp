namespace CSharpApp.Application.Products.Queries;

public sealed class GetProductQueryHandler(IProductsService productsService)
    : IRequestHandler<GetProductQuery, Product?>
{
    public async Task<Product?> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        return await productsService.GetProduct(request.Id);
    }
}
