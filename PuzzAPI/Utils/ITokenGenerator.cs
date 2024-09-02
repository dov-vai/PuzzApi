using PuzzAPI.Data.Models;

namespace PuzzAPI.Utils;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}