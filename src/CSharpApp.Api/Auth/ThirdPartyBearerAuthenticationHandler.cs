namespace CSharpApp.Api.Auth;

public sealed class ThirdPartyBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISender sender)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return AuthenticateResult.NoResult();
        }

        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("The Authorization header must use the Bearer scheme.");
        }

        var accessToken = authorization["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return AuthenticateResult.Fail("The bearer token is missing.");
        }

        AuthProfileResponse? profile;
        try
        {
            profile = await sender.Send(new GetAuthProfileQuery(accessToken));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Third-party profile validation failed.");
            return AuthenticateResult.Fail("The bearer token could not be validated.");
        }

        if (profile is null)
        {
            return AuthenticateResult.Fail("The bearer token is invalid.");
        }

        var claims = CreateClaims(profile);
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private static IEnumerable<Claim> CreateClaims(AuthProfileResponse profile)
    {
        if (profile.Id is not null)
        {
            yield return new Claim(ClaimTypes.NameIdentifier, profile.Id.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(profile.Email))
        {
            yield return new Claim(ClaimTypes.Email, profile.Email);
        }

        if (!string.IsNullOrWhiteSpace(profile.Name))
        {
            yield return new Claim(ClaimTypes.Name, profile.Name);
        }

        if (!string.IsNullOrWhiteSpace(profile.Role))
        {
            yield return new Claim(ClaimTypes.Role, profile.Role);
        }

        if (!string.IsNullOrWhiteSpace(profile.Avatar))
        {
            yield return new Claim(ThirdPartyBearerAuthenticationDefaults.AvatarClaimType, profile.Avatar);
        }
    }
}
