namespace CSharpApp.Application.Products;

public class ProductsService : IProductsService
{
    private readonly HttpClient _httpClient;
    private readonly RestApiSettings _restApiSettings;

    public ProductsService(HttpClient httpClient, IOptions<RestApiSettings> restApiSettings)
    {
        _httpClient = httpClient;
        _restApiSettings = restApiSettings.Value;
    }

    public async Task<IReadOnlyCollection<Product>> GetProducts()
    {
        var response = await _httpClient.GetAsync(_restApiSettings.Products);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<List<Product>>(content) 
            ?? throw new InvalidOperationException("Products API returned an empty or invalid response.");

        return products.AsReadOnly();
    }

    public async Task<Product?> GetProduct(int id)
    {
        var response = await _httpClient.GetAsync($"{_restApiSettings.Products}/{id}");
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Product>(content)
            ?? throw new InvalidOperationException($"Products API returned an empty or invalid response for product {id}.");
    }

    public async Task<Product?> CreateProduct(CreateProductRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(_restApiSettings.Products, request);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException(
                $"Products API rejected the create product request: {error}",
                null,
                response.StatusCode);
        }
        response.EnsureSuccessStatusCode();
        var createdProduct = await response.Content.ReadFromJsonAsync<Product>();
        return createdProduct
            ?? throw new InvalidOperationException("Products API returned an empty or invalid response after product creation.");
    }
}
