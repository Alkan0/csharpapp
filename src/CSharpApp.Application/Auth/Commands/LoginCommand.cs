namespace CSharpApp.Application.Auth.Commands;

public sealed record LoginCommand(LoginRequest Request) : IRequest<AuthTokenResponse?>;
