using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Tetris.Data;

public class TetrisDbContext : DbContext
{
    public DbSet<HighScoreEntry> HighScores => Set<HighScoreEntry>();

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
