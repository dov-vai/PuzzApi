using System.Data;
using System.Security.Authentication;
using PuzzAPI.Data.Repository;
using PuzzAPI.Models;
using PuzzAPI.Utils;

namespace PuzzAPI.Services;

public class AuthService
{
    private readonly JwtUtils _jwtUtils;
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository, JwtUtils jwtUtils)
    {
        _userRepository = userRepository;
        _jwtUtils = jwtUtils;
    }

    public async Task<string> Authenticate(User user)
    {
        var storedUser = await _userRepository.GetByUsername(user.Username);

        if (storedUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, storedUser.Password))
            throw new InvalidCredentialException();

        var token = _jwtUtils.GenerateToken(user);
        return token;
    }

    public async Task Register(User user)
    {
        var storedUser = await _userRepository.GetByUsername(user.Username);

        if (storedUser != null) throw new DuplicateNameException();

        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

        await _userRepository.Add(user);
    }
}