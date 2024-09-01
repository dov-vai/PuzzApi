using System.ComponentModel.DataAnnotations;

namespace PuzzAPI.Data.Models;

public class User
{
    [Key] public string Username { get; set; }

    public string Password { get; set; }
}