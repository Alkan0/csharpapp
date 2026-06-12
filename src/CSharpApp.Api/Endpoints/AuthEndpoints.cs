namespace CSharpApp.Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var versionedEndpointRouteBuilder = app.NewVersionedApi();

        versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/auth/login", async (LoginRequest request, ISender sender) =>
        {
            var token = await sender.Send(new LoginCommand(request));
            return token is null
                ? Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication failed",
                    detail: "The third-party authentication service rejected the provided credentials.")
                : Results.Ok(token);
        })
        .WithName("Login")
        .AddEndpointFilter<ValidationFilter<LoginRequest>>()
        .HasApiVersion(1.0)
        .WithTags("Auth");

        versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/auth/refresh-token", async (RefreshTokenRequest request, ISender sender) =>
        {
            var token = await sender.Send(new RefreshTokenCommand(request));
            return token is null
                ? Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Token refresh failed",
                    detail: "The third-party authentication service rejected the provided refresh token.")
                : Results.Ok(token);
        })
        .WithName("RefreshToken")
        .AddEndpointFilter<ValidationFilter<RefreshTokenRequest>>()
        .HasApiVersion(1.0)
        .WithTags("Auth");

        versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/auth/profile", (ClaimsPrincipal user) =>
        {
            var profile = new AuthProfileResponse
            {
                Id = int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null,
                Email = user.FindFirstValue(ClaimTypes.Email),
                Name = user.FindFirstValue(ClaimTypes.Name),
                Role = user.FindFirstValue(ClaimTypes.Role),
                Avatar = user.FindFirstValue(ThirdPartyBearerAuthenticationDefaults.AvatarClaimType)
            };

            return Results.Ok(profile);
        })
        .WithName("GetAuthProfile")
        .RequireAuthorization()
        .HasApiVersion(1.0)
        .WithTags("Auth");

        return app;
    }
}
