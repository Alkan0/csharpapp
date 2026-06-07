var builder = WebApplication.CreateBuilder(args);

var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Logging.ClearProviders().AddSerilog(logger);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDefaultConfiguration(builder.Configuration);
builder.Services.AddHttpConfiguration(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1.0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseMiddleware<RequestPerformanceMiddleware>();
app.MapHealthChecks("/health");

var versionedEndpointRouteBuilder = app.NewVersionedApi();

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/products", async (ISender sender) =>
{
    var products = await sender.Send(new GetProductsQuery());
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

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/categories", async (ICategoriesService categoriesService) =>
{
    var categories = await categoriesService.GetCategories();
    return Results.Ok(categories);
})
.WithName("GetCategories")
.HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/categories/{id:int}", async (int id, ICategoriesService categoriesService) =>
{
    var category = await categoriesService.GetCategory(id);

    return category is null
        ? Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Category not found",
            detail: $"Category with id {id} was not found.")
        : Results.Ok(category);
})
.WithName("GetCategory")
.HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/categories", async (CreateCategoryRequest request, ICategoriesService categoriesService) =>
{
    try
    {
        var category = await categoriesService.CreateCategory(request);
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
.HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPut("api/v{version:apiVersion}/categories/{id:int}", async (int id, UpdateCategoryRequest request, ICategoriesService categoriesService) =>
{
    var category = await categoriesService.UpdateCategory(id, request);
    return category is null
        ? Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Category update failed",
            detail: $"Category with id {id} was not found or could not be updated.")
        : Results.Ok(category);
})
.WithName("UpdateCategory")
.HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/auth/login", async (LoginRequest request, IAuthService authService) =>
{
    var token = await authService.Login(request);
    return token is null
        ? Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Authentication failed",
            detail: "The third-party authentication service rejected the provided credentials.")
        : Results.Ok(token);
})
.WithName("Login")
.HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/auth/profile", async (HttpRequest request, IAuthService authService) =>
{
    var authorization = request.Headers.Authorization.ToString();
    if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Authentication required",
            detail: "A bearer token is required to access the auth profile endpoint.");
    }
    var accessToken = authorization["Bearer ".Length..].Trim();
    var profile = await authService.GetProfile(accessToken);
    return profile is null
        ? Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Authentication failed",
            detail: "The third-party authentication service rejected the provided access token.")
        : Results.Ok(profile);
})
.WithName("GetAuthProfile")
.HasApiVersion(1.0);

app.Run();

public partial class Program;
