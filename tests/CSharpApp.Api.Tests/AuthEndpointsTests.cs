using CSharpApp.Core.Dtos;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CSharpApp.Api.Tests;

public sealed class AuthEndpointsTests(ApiTestApplicationFactory factory) : IClassFixture<ApiTestApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var request = new LoginRequest
        {
            Email = "john@mail.com",
            Password = "changeme"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.Equal("test-access-token", token?.AccessToken);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var request = new LoginRequest
        {
            Email = "john@mail.com",
            Password = "wrong"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidRequest_ReturnsBadRequest()
    {
        var request = new LoginRequest
        {
            Email = "not-an-email",
            Password = ""
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ReturnsToken()
    {
        var request = new RefreshTokenRequest
        {
            RefreshToken = "test-refresh-token"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", request);

        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.Equal("refreshed-access-token", token?.AccessToken);
        Assert.Equal("refreshed-refresh-token", token?.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_ReturnsUnauthorized()
    {
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRequest_ReturnsBadRequest()
    {
        var request = new RefreshTokenRequest
        {
            RefreshToken = ""
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithoutBearerToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithInvalidBearerToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await _client.GetAsync("/api/v1/auth/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithValidBearerToken_ReturnsProfile()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-access-token");

        var response = await _client.GetAsync("/api/v1/auth/profile");

        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<AuthProfileResponse>();
        Assert.Equal("john@mail.com", profile?.Email);
    }
}
