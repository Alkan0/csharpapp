namespace CSharpApp.Core.Dtos;

public sealed class RefreshTokenRequest
{
    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }
}
