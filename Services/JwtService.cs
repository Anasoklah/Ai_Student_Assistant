
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authentication.interfaces;
using Microsoft.IdentityModel.Tokens;
using SyrianStudyBot.Domain;

namespace Authentication.Services;

public class JwtService(IConfiguration configuration) : IJwtService
{
    private readonly IConfiguration _configuration = configuration;
    public string GenerateToken(ApplicationUser user)
    {
        // ensure user exsist 
        // create List of Claim including roles 
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique identifier for the token
        };
         // Assuming you have a way to get the user's roles
        // take the secret key from appsettings and create SymmetricSecurityKey
         var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)); // Get this from configuration
        
        // create signing credentials using the key and HmacSha256 algorithm

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        // create the token descriptor with claims, expiry, signing credentials
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:DurationInMinutes"]!)),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = credentials
        };
        // create the token handler and generate the token string
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public int GetAccessTokenExpirationMinutes()
    {
        return _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);
    }
}
