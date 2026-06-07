namespace CSharpApp.Application.Categories.Queries;

public sealed class GetCategoryQueryHandler(ICategoriesService categoriesService)
    : IRequestHandler<GetCategoryQuery, Category?>
{
    public async Task<Category?> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
    {
        return await categoriesService.GetCategory(request.Id);
    }
}
