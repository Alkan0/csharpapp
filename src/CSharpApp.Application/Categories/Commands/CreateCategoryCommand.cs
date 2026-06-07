namespace CSharpApp.Application.Categories.Commands;

public sealed record CreateCategoryCommand(CreateCategoryRequest Request) : IRequest<Category?>;
