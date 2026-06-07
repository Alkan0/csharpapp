namespace CSharpApp.Application.Products.Commands;

public sealed class CreateProductCommandHandler(IProductsService productsService)
    : IRequestHandler<CreateProductCommand, Product?>
{
    public async Task<Product?> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        return await productsService.CreateProduct(request.Request);
    }
}
