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

    public async Task<string> Login(User user)
    {
        var storedUser = await _userRepository.GetByUsername(user.Username);

        if (storedUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, storedUser.Password))
            throw new InvalidCredentialException();

        var token = _tokenUtils.GenerateToken(user);
        return token;
    }

    public async Task Register(User user)
    {
        var storedUser = await _userRepository.GetByUsername(user.Username);

        if (storedUser != null) throw new DuplicateNameException();

        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

        await _userRepository.Add(user);
    }

    public async Task<User?> Authenticate(string? token)
    {
        if (token == null || !_tokenUtils.ValidateToken(token))
            return null;

        var username = _tokenUtils.GetUsernameFromToken(token);

        return await _userRepository.GetByUsername(username);
    }
}