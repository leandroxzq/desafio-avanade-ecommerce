using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AuthService.Models;

namespace AuthService.Services;

public class TokenService
{
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public TokenService(IConfiguration config)
    {
        _jwtKey = config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key");
         _jwtIssuer = config["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer");
        _jwtAudience = config["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience");
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience 
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
