namespace CSharpApp.Core.Interfaces;

public interface IAuthService
{
    Task<AuthTokenResponse?> Login(LoginRequest request);
}
