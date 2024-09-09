using PuzzAPI.Data.Models;

namespace PuzzAPI.Utils;

public interface ITokenUtils
{
    string GenerateToken(User user);
    bool ValidateToken(string token);
    string GetUsernameFromToken(string token);
}