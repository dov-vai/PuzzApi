using Microsoft.EntityFrameworkCore;
using PuzzAPI.Data.Contexts;
using PuzzAPI.Data.Models;

namespace PuzzAPI.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserContext _context;
    private readonly DbSet<User> _users;

    public UserRepository(UserContext context)
    {
        _context = context;
        _users = context.Set<User>();
    }

    public async Task<User?> GetByUsername(string username)
    {
        return await _users.FindAsync(username);
    }

    public async Task<bool> Add(User user)
    {
        if (_users.Any(u => u.Username == user.Username)) return false;

        await _users.AddAsync(user);
        await _context.SaveChangesAsync();

        return true;
    }
}