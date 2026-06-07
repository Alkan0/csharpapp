namespace CSharpApp.Application.Categories.Commands;

public sealed record UpdateCategoryCommand(int Id, UpdateCategoryRequest Request) : IRequest<Category?>;
