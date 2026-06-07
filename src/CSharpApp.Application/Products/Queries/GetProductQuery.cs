namespace CSharpApp.Application.Products.Queries;

public sealed record GetProductQuery(int Id) : IRequest<Product?>;
