using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PuzzAPI.Data.Models;

namespace PuzzAPI.Utils;

public class JwtUtils : ITokenUtils
{
    private readonly RsaKeyProvider _keyProvider;

    public JwtUtils(RsaKeyProvider keyProvider)
    {
        _keyProvider = keyProvider;
    }

    public string GenerateToken(User user, DateTime? expires)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(
            _keyProvider.RsaSecurityKey,
            SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = GenerateClaims(user),
            Expires = expires,
            SigningCredentials = credentials
        };

        var token = handler.CreateToken(tokenDescriptor);

        return handler.WriteToken(token);
    }

    public bool ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _keyProvider.RsaSecurityKey,
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch (SecurityTokenExpiredException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    public string GetUsernameFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        Debug.WriteLine(jwtToken.Claims.ToString());
        return jwtToken.Claims.First(c => c.Type == "unique_name").Value;
    }

    private static ClaimsIdentity GenerateClaims(User user)
    {
        var claims = new ClaimsIdentity();
        claims.AddClaim(new Claim(ClaimTypes.Name, user.Username));

        return claims;
    }
}