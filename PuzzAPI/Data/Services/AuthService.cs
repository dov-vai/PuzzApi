using System.Data;
using System.Security.Authentication;
using PuzzAPI.Data.Models;
using PuzzAPI.Data.Repositories;
using PuzzAPI.Utils;

namespace PuzzAPI.Data.Services;

public class AuthService
{
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository, ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<string> Authenticate(User user)
    {
        var storedUser = await _userRepository.GetByUsername(user.Username);

        if (storedUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, storedUser.Password))
            throw new InvalidCredentialException();

        var token = _tokenGenerator.GenerateToken(user);
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