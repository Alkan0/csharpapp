namespace CSharpApp.Application.Categories.Commands;

public sealed class DeleteCategoryCommandHandler(ICategoriesService categoriesService)
    : IRequestHandler<DeleteCategoryCommand, bool>
{
    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        return await categoriesService.DeleteCategory(request.Id);
    }
}
