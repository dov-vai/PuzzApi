using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PuzzAPI.Models;

namespace PuzzAPI.Utils;

public class JwtUtils
{
    private readonly RsaKeyProvider _keyProvider;

    public JwtUtils(RsaKeyProvider keyProvider)
    {
        _keyProvider = keyProvider;
    }

    public string GenerateToken(User user)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(
            _keyProvider.RsaSecurityKey,
            SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = GenerateClaims(user),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = credentials
        };

        var token = handler.CreateToken(tokenDescriptor);

        return handler.WriteToken(token);
    }

    private static ClaimsIdentity GenerateClaims(User user)
    {
        var claims = new ClaimsIdentity();
        claims.AddClaim(new Claim(ClaimTypes.Name, user.Username));

        return claims;
    }
}