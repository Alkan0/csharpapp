namespace CSharpApp.Application.Categories;

public class CategoriesService : ICategoriesService
{
    private readonly HttpClient _httpClient;
    private readonly RestApiSettings _restApiSettings;

    public CategoriesService(
        HttpClient httpClient,
        IOptions<RestApiSettings> restApiSettings)
    {
        _httpClient = httpClient;
        _restApiSettings = restApiSettings.Value;
    }

    public async Task<IReadOnlyCollection<Category>> GetCategories()
    {
        var response = await _httpClient.GetAsync(_restApiSettings.Categories);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var categories = JsonSerializer.Deserialize<List<Category>>(content)
            ?? throw new InvalidOperationException("Categories API returned an empty or invalid response.");
        return categories.AsReadOnly();
    }

    public async Task<Category?> GetCategory(int id)
    {
        var response = await _httpClient.GetAsync($"{_restApiSettings.Categories}/{id}");
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Category>(content)
            ?? throw new InvalidOperationException($"Categories API returned an empty or invalid response for category {id}.");
    }

    public async Task<Category?> CreateCategory(CreateCategoryRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(_restApiSettings.Categories, request);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Categories API rejected the create category request: {error}",
                null,
                response.StatusCode);
        }
        response.EnsureSuccessStatusCode();
        var createdCategory = await response.Content.ReadFromJsonAsync<Category>();
        return createdCategory
            ?? throw new InvalidOperationException("Categories API returned an empty or invalid response after category creation.");
    }
    public async Task<Category?> UpdateCategory(int id, UpdateCategoryRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_restApiSettings.Categories}/{id}", request);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        var updatedCategory = await response.Content.ReadFromJsonAsync<Category>();
        return updatedCategory
            ?? throw new InvalidOperationException($"Categories API returned an empty or invalid response after updating category {id}.");
    }

    public async Task<bool> DeleteCategory(int id)
    {
        var response = await _httpClient.DeleteAsync($"{_restApiSettings.Categories}/{id}");
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<IReadOnlyCollection<Product>> GetCategoryProducts(int id)
    {
        var response = await _httpClient.GetAsync($"{_restApiSettings.Categories}/{id}/{_restApiSettings.Products}");
        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<List<Product>>()
            ?? throw new InvalidOperationException($"Categories API returned an empty or invalid products response for category {id}.");

        return products.AsReadOnly();
    }
}
