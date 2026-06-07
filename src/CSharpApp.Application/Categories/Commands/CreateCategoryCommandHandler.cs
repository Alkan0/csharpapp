namespace CSharpApp.Application.Categories.Commands;

public sealed class CreateCategoryCommandHandler(ICategoriesService categoriesService)
    : IRequestHandler<CreateCategoryCommand, Category?>
{
    public async Task<Category?> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        return await categoriesService.CreateCategory(request.Request);
    }
}
