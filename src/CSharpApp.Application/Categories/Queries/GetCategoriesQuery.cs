namespace CSharpApp.Application.Categories.Queries;

public sealed record GetCategoriesQuery : IRequest<IReadOnlyCollection<Category>>;
