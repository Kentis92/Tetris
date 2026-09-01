using System.IO;
using Microsoft.EntityFrameworkCore;
using Tetris.Models;

namespace Tetris.Data;

public class TetrisDbContext : DbContext
{
    public DbSet<HighScoreEntry> HighScores => Set<HighScoreEntry>();
    public DbSet<User> Users => Set<User>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tetris"
        );

        Directory.CreateDirectory(folder);

        string databasePath = Path.Combine(folder, "tetris.db");

        optionsBuilder.UseSqlite($"Data Source={databasePath}");
    }
}
