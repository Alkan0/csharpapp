namespace CSharpApp.Application.Auth.Commands;

public sealed record RefreshTokenCommand(RefreshTokenRequest Request) : IRequest<AuthTokenResponse?>;
