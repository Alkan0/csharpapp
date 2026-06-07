namespace CSharpApp.Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var versionedEndpointRouteBuilder = app.NewVersionedApi();

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
        .AddEndpointFilter<ValidationFilter<LoginRequest>>()
        .HasApiVersion(1.0)
        .WithTags("Auth");

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
        .HasApiVersion(1.0)
        .WithTags("Auth");

        return app;
    }
}
