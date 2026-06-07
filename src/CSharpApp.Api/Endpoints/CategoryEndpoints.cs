namespace CSharpApp.Api.Endpoints;

public static class CategoryEndpoints
{
    public static WebApplication MapCategoryEndpoints(this WebApplication app)
    {
        var versionedEndpointRouteBuilder = app.NewVersionedApi();

        versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/categories", async (ISender sender) =>
        {
            var categories = await sender.Send(new GetCategoriesQuery());
            return Results.Ok(categories);
        })
        .WithName("GetCategories")
        .HasApiVersion(1.0)
        .WithTags("Categories");

        versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/categories/{id:int}", async (int id, ISender sender) =>
        {
            var category = await sender.Send(new GetCategoryQuery(id));

            return category is null
                ? Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Category not found",
                    detail: $"Category with id {id} was not found.")
                : Results.Ok(category);
        })
        .WithName("GetCategory")
        .HasApiVersion(1.0)
        .WithTags("Categories");

        versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/categories", async (CreateCategoryRequest request, ISender sender) =>
        {
            try
            {
                var category = await sender.Send(new CreateCategoryCommand(request));
                return Results.Created($"api/v1/categories/{category?.Id}", category);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Category creation failed",
                    detail: ex.Message);
            }
        })
        .WithName("CreateCategory")
        .AddEndpointFilter<ValidationFilter<CreateCategoryRequest>>()
        .HasApiVersion(1.0)
        .WithTags("Categories");

        versionedEndpointRouteBuilder.MapPut("api/v{version:apiVersion}/categories/{id:int}", async (int id, UpdateCategoryRequest request, ISender sender) =>
        {
            var category = await sender.Send(new UpdateCategoryCommand(id, request));
            return category is null
                ? Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Category update failed",
                    detail: $"Category with id {id} was not found or could not be updated.")
                : Results.Ok(category);
        })
        .WithName("UpdateCategory")
        .AddEndpointFilter<ValidationFilter<UpdateCategoryRequest>>()
        .HasApiVersion(1.0)
        .WithTags("Categories");

        versionedEndpointRouteBuilder.MapDelete("api/v{version:apiVersion}/categories/{id:int}", async (int id, ISender sender) =>
        {
            var deleted = await sender.Send(new DeleteCategoryCommand(id));
            return deleted
                ? Results.NoContent()
                : Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Category deletion failed",
                    detail: $"Category with id {id} was not found or could not be deleted.");
        })
        .WithName("DeleteCategory")
        .HasApiVersion(1.0)
        .WithTags("Categories");

        versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/categories/{id:int}/products", async (int id, ISender sender) =>
        {
            var products = await sender.Send(new GetCategoryProductsQuery(id));
            return Results.Ok(products);
        })
        .WithName("GetCategoryProducts")
        .HasApiVersion(1.0)
        .WithTags("Categories");

        return app;
    }
}
