namespace CSharpApp.Application.Categories.Commands;

public sealed record DeleteCategoryCommand(int Id) : IRequest<bool>;
