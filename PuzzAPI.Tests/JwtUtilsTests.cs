using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Moq;
using PuzzAPI.Data.Models;
using PuzzAPI.Utils;

namespace PuzzAPI.Tests;

public class JwtUtilsTests
{
    [Fact]
    public void GenerateToken_ShouldReturnValidJwtToken()
    {
        // Arrange
        var user = new User { Username = "verypublicuser", Password = "topsecretpassword" };
        var rsaKeyProvider = new RsaKeyProvider(new RsaSecurityKey(RSA.Create(2048)));
        var jwtUtils = new JwtUtils(rsaKeyProvider);

        // Act
        var token = jwtUtils.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.NotNull(jwtToken);

        // Validate expiration
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
        Assert.True(jwtToken.ValidTo <= DateTime.UtcNow.AddMinutes(15));
    }
}