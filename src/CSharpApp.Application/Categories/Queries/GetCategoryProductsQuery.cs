namespace CSharpApp.Application.Categories.Queries;

public sealed record GetCategoryProductsQuery(int Id) : IRequest<IReadOnlyCollection<Product>>;
