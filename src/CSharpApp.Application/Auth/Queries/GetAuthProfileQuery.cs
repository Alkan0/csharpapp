namespace CSharpApp.Application.Auth.Queries;

public sealed record GetAuthProfileQuery(string AccessToken) : IRequest<AuthProfileResponse?>;
