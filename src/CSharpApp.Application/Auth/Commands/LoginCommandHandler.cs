namespace CSharpApp.Application.Auth.Commands;

public sealed class LoginCommandHandler(IAuthService authService)
    : IRequestHandler<LoginCommand, AuthTokenResponse?>
{
    public async Task<AuthTokenResponse?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return await authService.Login(request.Request);
    }
}
