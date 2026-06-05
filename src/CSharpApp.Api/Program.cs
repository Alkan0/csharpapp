var builder = WebApplication.CreateBuilder(args);

var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Logging.ClearProviders().AddSerilog(logger);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDefaultConfiguration();
builder.Services.AddHttpConfiguration();
builder.Services.AddProblemDetails();
builder.Services.AddApiVersioning();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

var versionedEndpointRouteBuilder = app.NewVersionedApi();

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/products", async (IProductsService productsService) =>
{
    var products = await productsService.GetProducts();
    return Results.Ok(products);
})
.WithName("GetProducts")
.HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/products/{id:int}", async (int id, IProductsService productsService) =>
{
    var product = await productsService.GetProduct(id);

    return product is null
        ? Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Product not found",
            detail: $"Product with id {id} was not found.")
        : Results.Ok(product);
})
.WithName("GetProduct")
.HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/products", async (CreateProductRequest request, IProductsService productsService) =>
{
    try
    {
        var product = await productsService.CreateProduct(request);
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
.HasApiVersion(1.0);

app.Run();