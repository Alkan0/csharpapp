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

        versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/auth/profile", async (HttpRequest request, ISender sender) =>
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
            var profile = await sender.Send(new GetAuthProfileQuery(accessToken));
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
