namespace CSharpApp.Application.Tests.Auth;

public sealed class AuthHandlersTests
{
    [Fact]
    public async Task LoginCommandHandler_ReturnsTokenFromService()
    {
        var service = new FakeAuthService();
        var handler = new LoginCommandHandler(service);
        var request = new LoginRequest
        {
            Email = "john@mail.com",
            Password = "changeme"
        };

        var token = await handler.Handle(new LoginCommand(request), CancellationToken.None);

        Assert.NotNull(token);
        Assert.Equal("access-token", token.AccessToken);
        Assert.Same(request, service.LastLoginRequest);
    }

    [Fact]
    public async Task GetAuthProfileQueryHandler_ReturnsProfileFromService()
    {
        var service = new FakeAuthService();
        var handler = new GetAuthProfileQueryHandler(service);

        var profile = await handler.Handle(new GetAuthProfileQuery("access-token"), CancellationToken.None);

        Assert.NotNull(profile);
        Assert.Equal("john@mail.com", profile.Email);
        Assert.Equal("access-token", service.LastAccessToken);
    }

    [Fact]
    public async Task RefreshTokenCommandHandler_ReturnsTokenFromService()
    {
        var service = new FakeAuthService();
        var handler = new RefreshTokenCommandHandler(service);
        var request = new RefreshTokenRequest
        {
            RefreshToken = "refresh-token"
        };

        var token = await handler.Handle(new RefreshTokenCommand(request), CancellationToken.None);

        Assert.NotNull(token);
        Assert.Equal("refreshed-access-token", token.AccessToken);
        Assert.Same(request, service.LastRefreshTokenRequest);
    }

    private sealed class FakeAuthService : IAuthService
    {
        public LoginRequest? LastLoginRequest { get; private set; }
        public RefreshTokenRequest? LastRefreshTokenRequest { get; private set; }
        public string? LastAccessToken { get; private set; }

        public Task<AuthTokenResponse?> Login(LoginRequest request)
        {
            LastLoginRequest = request;

            return Task.FromResult<AuthTokenResponse?>(new AuthTokenResponse
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token"
            });
        }

        public Task<AuthTokenResponse?> RefreshToken(RefreshTokenRequest request)
        {
            LastRefreshTokenRequest = request;

            return Task.FromResult<AuthTokenResponse?>(new AuthTokenResponse
            {
                AccessToken = "refreshed-access-token",
                RefreshToken = "refreshed-refresh-token"
            });
        }

        public Task<AuthProfileResponse?> GetProfile(string accessToken)
        {
            LastAccessToken = accessToken;

            return Task.FromResult<AuthProfileResponse?>(new AuthProfileResponse
            {
                Id = 1,
                Email = "john@mail.com",
                Name = "John Doe",
                Role = "customer",
                Avatar = "https://example.com/avatar.png"
            });
        }
    }
}
