namespace CSharpApp.Application.Categories.Commands;

public sealed class UpdateCategoryCommandHandler(ICategoriesService categoriesService)
    : IRequestHandler<UpdateCategoryCommand, Category?>
{
    public async Task<Category?> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        return await categoriesService.UpdateCategory(request.Id, request.Request);
    }
}
