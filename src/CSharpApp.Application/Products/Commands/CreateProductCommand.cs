namespace CSharpApp.Application.Products.Commands;

public sealed record CreateProductCommand(CreateProductRequest Request) : IRequest<Product?>;
