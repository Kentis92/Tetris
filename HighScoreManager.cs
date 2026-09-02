using Microsoft.EntityFrameworkCore;
using Tetris.Data;

namespace Tetris;

public class HighScoreEntry
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Score { get; set; }
}

public class HighScoreManager
{
    private const int MaxScores = 10;

    public List<HighScoreEntry> LoadScores()
    {
        using TetrisDbContext db = new();

        return db.HighScores.OrderByDescending(x => x.Score).Take(MaxScores).ToList();
    }

    public void SaveScore(string name, int score)
    {
        using TetrisDbContext db = new();

        db.HighScores.Add(new HighScoreEntry { Name = name, Score = score });

        db.SaveChanges();

        List<HighScoreEntry> scores = db.HighScores.OrderByDescending(x => x.Score).ToList();

        if (scores.Count > MaxScores)
        {
            db.HighScores.RemoveRange(scores.Skip(MaxScores));
            db.SaveChanges();
        }
    }

    public int GetPlayerBestScore(string username)
    {
        using TetrisDbContext db = new();

        return db.HighScores.Where(x => x.Name == username).Select(x => (int?)x.Score).Max() ?? 0;
    }

    public int GetPlayerScoreCount(string username)
    {
        using TetrisDbContext db = new();

        return db.HighScores.Count(x => x.Name == username);
    }
}
