using System.Data;
using System.Security.Authentication;
using PuzzAPI.Data.Models;
using PuzzAPI.Data.Repositories;
using PuzzAPI.Utils;

namespace PuzzAPI.Data.Services;

public class AuthService
{
    private readonly ITokenUtils _tokenUtils;
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository, ITokenUtils tokenUtils)
    {
        _userRepository = userRepository;
        _tokenUtils = tokenUtils;
    }

    public async Task<UserTokens> Login(User user)
    {
        var storedUser = await _userRepository.GetByUsername(user.Username);

        if (storedUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, storedUser.Password))
            throw new InvalidCredentialException();

        var token = _tokenUtils.GenerateToken(user, DateTime.UtcNow.AddMinutes(15));
        var refreshToken = _tokenUtils.GenerateToken(user, DateTime.UtcNow.AddDays(15));
        storedUser.RefreshToken = refreshToken;
        await _userRepository.Update(storedUser);

        return new UserTokens { AuthToken = token, RefreshToken = refreshToken };
    }

    public async Task Register(User user)
    {
        var storedUser = await _userRepository.GetByUsername(user.Username);
        if (storedUser != null) throw new DuplicateNameException();
        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        await _userRepository.Add(user);
    }

    public async Task<User?> Authenticate(string token)
    {
        if (!_tokenUtils.ValidateToken(token))
            return null;

        var username = _tokenUtils.GetUsernameFromToken(token);

        return await _userRepository.GetByUsername(username);
    }

    public async Task<UserTokens?> RefreshToken(string refreshToken)
    {
        if (!_tokenUtils.ValidateToken(refreshToken))
            return null;

        var username = _tokenUtils.GetUsernameFromToken(refreshToken);

        var user = await _userRepository.GetByUsername(username);

        if (user?.RefreshToken == refreshToken)
        {
            var token = _tokenUtils.GenerateToken(user, DateTime.UtcNow.AddMinutes(15));
            return new UserTokens { AuthToken = token, RefreshToken = refreshToken };
        }

        return null;
    }

    public async Task InvalidateRefreshToken(string refreshToken)
    {
        if (!_tokenUtils.ValidateToken(refreshToken))
            return;

        var username = _tokenUtils.GetUsernameFromToken(refreshToken);
        var user = await _userRepository.GetByUsername(username);
        if (user?.RefreshToken == refreshToken)
        {
            user.RefreshToken = null;
            await _userRepository.Update(user);
        }
    }
}