using PuzzAPI.Models;

namespace PuzzAPI.Data.Repository;

public interface IUserRepository
{
    Task<User?> GetByUsername(string username);
    Task<bool> Add(User user);
}