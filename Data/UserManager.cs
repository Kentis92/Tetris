using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Tetris.Data;
using Tetris.Models;

namespace Tetris;

public class UserManager
{
    public bool Register(string username, string password)
    {
        using TetrisDbContext db = new();

        if (db.Users.Any(x => x.Username == username))
        {
            return false;
        }

        string passwordHash = HashPassword(password);

        db.Users.Add(
            new User
            {
                Username = username,
                PasswordHash = passwordHash,
                Role = "Player",
            }
        );

        db.SaveChanges();

        return true;
    }

    public User? Login(string username, string password)
    {
        using TetrisDbContext db = new();

        User? user = db.Users.FirstOrDefault(x => x.Username == username);

        if (user == null)
        {
            return null;
        }

        return VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    private string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100000,
            HashAlgorithmName.SHA256,
            32
        );

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        string[] parts = passwordHash.Split(':');

        if (parts.Length != 2)
        {
            return false;
        }

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] storedHash = Convert.FromBase64String(parts[1]);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100000,
            HashAlgorithmName.SHA256,
            32
        );

        return CryptographicOperations.FixedTimeEquals(hash, storedHash);
    }
}
