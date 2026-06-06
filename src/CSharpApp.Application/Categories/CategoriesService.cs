namespace CSharpApp.Application.Categories;

public class CategoriesService : ICategoriesService
{
    private readonly HttpClient _httpClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<CategoriesService> _logger;

    public CategoriesService(
        HttpClient httpClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<CategoriesService> logger)
    {
        _httpClient = httpClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
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
            _logger.LogInformation("Category {CategoryId} was not found", id);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Category>(content)
            ?? throw new InvalidOperationException($"Categories API returned an empty or invalid response for category {id}.");
    }
}