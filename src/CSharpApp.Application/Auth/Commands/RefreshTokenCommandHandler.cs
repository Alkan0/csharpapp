namespace CSharpApp.Application.Auth.Commands;

public sealed class RefreshTokenCommandHandler(IAuthService authService)
    : IRequestHandler<RefreshTokenCommand, AuthTokenResponse?>
{
    public async Task<AuthTokenResponse?> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return await authService.RefreshToken(request.Request);
    }
}
