namespace CSharpApp.Application.Products.Queries;

public sealed record GetProductsQuery : IRequest<IReadOnlyCollection<Product>>;
