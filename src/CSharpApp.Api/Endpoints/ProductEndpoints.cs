namespace CSharpApp.Api.Endpoints;

public static class ProductEndpoints
{
    public static WebApplication MapProductEndpoints(this WebApplication app)
    {
        var versionedEndpointRouteBuilder = app.NewVersionedApi();

        versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/products", async (ISender sender) =>
        {
            var products = await sender.Send(new GetProductsQuery());
            return Results.Ok(products);
        })
        .WithName("GetProducts")
        .HasApiVersion(1.0)
        .WithTags("Products");

        versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/products/{id:int}", async (int id, ISender sender) =>
        {
            var product = await sender.Send(new GetProductQuery(id));

            return product is null
                ? Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Product not found",
                    detail: $"Product with id {id} was not found.")
                : Results.Ok(product);
        })
        .WithName("GetProduct")
        .HasApiVersion(1.0)
        .WithTags("Products");

        versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/products", async (CreateProductRequest request, ISender sender) =>
        {
            try
            {
                var product = await sender.Send(new CreateProductCommand(request));
                return Results.Created($"api/v1/products/{product?.Id}", product);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Product creation failed",
                    detail: ex.Message);
            }
        })
        .WithName("CreateProduct")
        .AddEndpointFilter<ValidationFilter<CreateProductRequest>>()
        .HasApiVersion(1.0)
        .WithTags("Products");

        return app;
    }
}
