

namespace Authentication.Dtos;

public class AuthResponse
{
    public bool isSuccess { get; set; }
    public string? Message { get; set; }
    public string? UserName { get; set; }
    public Guid? userId { get; set; }
    public string? Email { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? AccessTokenExpiry { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
}