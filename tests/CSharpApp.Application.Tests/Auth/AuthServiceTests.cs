namespace CSharpApp.Application.Tests.Auth;

public class AuthServiceTests
{
    [Fact]
    public async Task Login_ReturnsToken_WhenApiReturnsValidResponse()
    {
        var service = CreateService(_ => TestHttpMessageHandler.Json(HttpStatusCode.OK, """
            {
              "access_token": "access-token",
              "refresh_token": "refresh-token"
            }
            """));

        var token = await service.Login(new LoginRequest
        {
            Email = "john@mail.com",
            Password = "changeme"
        });

        Assert.NotNull(token);
        Assert.Equal("access-token", token.AccessToken);
        Assert.Equal("refresh-token", token.RefreshToken);
    }

    [Fact]
    public async Task Login_ReturnsNull_WhenApiReturnsUnauthorized()
    {
        var service = CreateService(_ => TestHttpMessageHandler.Json(HttpStatusCode.Unauthorized, "{}"));

        var token = await service.Login(new LoginRequest
        {
            Email = "john@mail.com",
            Password = "wrong-password"
        });

        Assert.Null(token);
    }

    [Fact]
    public async Task RefreshToken_ReturnsToken_WhenApiReturnsValidResponse()
    {
        HttpRequestMessage? capturedRequest = null;
        var service = CreateService(request =>
        {
            capturedRequest = request;

            return TestHttpMessageHandler.Json(HttpStatusCode.OK, """
                {
                  "access_token": "new-access-token",
                  "refresh_token": "new-refresh-token"
                }
                """);
        });

        var token = await service.RefreshToken(new RefreshTokenRequest
        {
            RefreshToken = "refresh-token"
        });

        Assert.NotNull(token);
        Assert.Equal("new-access-token", token.AccessToken);
        Assert.Equal("new-refresh-token", token.RefreshToken);
        Assert.Equal("/api/v1/auth/refresh-token", capturedRequest?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task RefreshToken_ReturnsNull_WhenApiReturnsUnauthorized()
    {
        var service = CreateService(_ => TestHttpMessageHandler.Json(HttpStatusCode.Unauthorized, "{}"));

        var token = await service.RefreshToken(new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        });

        Assert.Null(token);
    }

    [Fact]
    public async Task GetProfile_SendsBearerToken()
    {
        HttpRequestMessage? capturedRequest = null;
        var service = CreateService(request =>
        {
            capturedRequest = request;

            return TestHttpMessageHandler.Json(HttpStatusCode.OK, """
                {
                  "id": 1,
                  "email": "john@mail.com",
                  "name": "John",
                  "role": "customer",
                  "avatar": "https://example.com/avatar.jpg"
                }
                """);
        });

        var profile = await service.GetProfile("access-token");

        Assert.NotNull(profile);
        Assert.Equal("john@mail.com", profile.Email);
        Assert.Equal("Bearer", capturedRequest?.Headers.Authorization?.Scheme);
        Assert.Equal("access-token", capturedRequest?.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task GetProfile_ReturnsNull_WhenApiReturnsUnauthorized()
    {
        var service = CreateService(_ => TestHttpMessageHandler.Json(HttpStatusCode.Unauthorized, "{}"));

        var profile = await service.GetProfile("invalid-token");

        Assert.Null(profile);
    }

    private static AuthService CreateService(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var httpClient = new HttpClient(new TestHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://example.com/api/v1/")
        };

        var settings = Options.Create(new RestApiSettings
        {
            Auth = "auth/login",
            AuthRefreshToken = "auth/refresh-token",
            AuthProfile = "auth/profile"
        });

        return new AuthService(httpClient, settings);
    }
}
