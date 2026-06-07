namespace CSharpApp.Application.Categories.Queries;

public sealed record GetCategoryQuery(int Id) : IRequest<Category?>;
