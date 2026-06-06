namespace CSharpApp.Application.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        HttpClient httpClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<AuthTokenResponse?> Login(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(_restApiSettings.Auth, request);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Third-party auth rejected login request.");
            return null;
        }
        response.EnsureSuccessStatusCode();
        var authToken = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        return authToken
            ?? throw new InvalidOperationException("Auth API returned an empty or invalid response.");
    }
}