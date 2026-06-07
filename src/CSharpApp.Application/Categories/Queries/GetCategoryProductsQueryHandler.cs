namespace CSharpApp.Application.Categories.Queries;

public sealed class GetCategoryProductsQueryHandler(ICategoriesService categoriesService)
    : IRequestHandler<GetCategoryProductsQuery, IReadOnlyCollection<Product>>
{
    public async Task<IReadOnlyCollection<Product>> Handle(GetCategoryProductsQuery request, CancellationToken cancellationToken)
    {
        return await categoriesService.GetCategoryProducts(request.Id);
    }
}
