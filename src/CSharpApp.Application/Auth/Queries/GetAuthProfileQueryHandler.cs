namespace CSharpApp.Application.Auth.Queries;

public sealed class GetAuthProfileQueryHandler(IAuthService authService)
    : IRequestHandler<GetAuthProfileQuery, AuthProfileResponse?>
{
    public async Task<AuthProfileResponse?> Handle(GetAuthProfileQuery request, CancellationToken cancellationToken)
    {
        return await authService.GetProfile(request.AccessToken);
    }
}
