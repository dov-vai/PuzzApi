using PuzzAPI.Data.Models;

namespace PuzzAPI.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsername(string username);
    Task<bool> Add(User user);
    Task Update(User user);
}