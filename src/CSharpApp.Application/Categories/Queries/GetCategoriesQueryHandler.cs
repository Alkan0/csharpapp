namespace CSharpApp.Application.Categories.Queries;

public sealed class GetCategoriesQueryHandler(ICategoriesService categoriesService)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyCollection<Category>>
{
    public async Task<IReadOnlyCollection<Category>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await categoriesService.GetCategories();
    }
}
