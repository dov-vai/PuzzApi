using System.Data;
using System.Security.Authentication;
using BCrypt.Net;
using Moq;
using PuzzAPI.Data.Models;
using PuzzAPI.Data.Repositories;
using PuzzAPI.Data.Services;
using PuzzAPI.Utils;

namespace PuzzAPI.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenGeneratorMock = new Mock<ITokenGenerator>();
        _authService = new AuthService(_userRepositoryMock.Object, _tokenGeneratorMock.Object);
    }

    [Fact]
    public async Task Authenticate_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var user = new User { Username = "validUser", Password = "validPassword" };
        var storedUser = new User { Username = "validUser", Password = BCrypt.Net.BCrypt.HashPassword("validPassword") };

        _userRepositoryMock.Setup(repo => repo.GetByUsername(user.Username))
                           .ReturnsAsync(storedUser);
        _tokenGeneratorMock.Setup(jwt => jwt.GenerateToken(user))
                     .Returns("validToken");

        // Act
        var result = await _authService.Authenticate(user);

        // Assert
        Assert.Equal("validToken", result);
    }

    [Fact]
    public async Task Authenticate_ShouldThrowInvalidCredentialException_WhenUserDoesNotExist()
    {
        // Arrange
        var user = new User { Username = "nonexistentUser", Password = "password" };

        _userRepositoryMock.Setup(repo => repo.GetByUsername(user.Username))
                           .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialException>(() => _authService.Authenticate(user));
    }

    [Fact]
    public async Task Authenticate_ShouldThrowInvalidCredentialException_WhenPasswordIsInvalid()
    {
        // Arrange
        var user = new User { Username = "validUser", Password = "invalidPassword" };
        var storedUser = new User { Username = "validUser", Password = BCrypt.Net.BCrypt.HashPassword("validPassword") };

        _userRepositoryMock.Setup(repo => repo.GetByUsername(user.Username))
                           .ReturnsAsync(storedUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialException>(() => _authService.Authenticate(user));
    }

    [Fact]
    public async Task Register_ShouldHashPasswordAndAddUser_WhenUserDoesNotExist()
    {
        // Arrange
        var user = new User { Username = "newUser", Password = "password" };
    
        _userRepositoryMock.Setup(repo => repo.GetByUsername(user.Username))
                           .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(repo => repo.Add(It.IsAny<User>()))
                           .ReturnsAsync(true);
    
        // Act
        await _authService.Register(user);
    
        // Assert
        _userRepositoryMock.Verify(repo => 
            repo.Add(
                It.Is<User>(u => 
                    u.Username == "newUser" && BCrypt.Net.BCrypt.Verify("password", u.Password, false, HashType.SHA384))),
            Times.Once);
    }

    [Fact]
    public async Task Register_ShouldThrowDuplicateNameException_WhenUserAlreadyExists()
    {
        // Arrange
        var user = new User { Username = "existingUser", Password = "password" };
        var storedUser = new User { Username = "existingUser", Password = "hashedPassword" };

        _userRepositoryMock.Setup(repo => repo.GetByUsername(user.Username))
                           .ReturnsAsync(storedUser);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateNameException>(() => _authService.Register(user));
    }
}