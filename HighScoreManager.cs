using Microsoft.EntityFrameworkCore;
using Tetris.Data;
using Tetris.Models;

namespace Tetris;

public class HighScoreManager
{
    private const int MaxScores = 10;

    public List<HighScoreEntry> LoadScores(string gameMode)
    {
        using TetrisDbContext db = new();

        IQueryable<HighScoreEntry> query = db.HighScores.Where(x => x.GameMode == gameMode);

        if (gameMode == GameMode.Sprint.ToString())
        {
            return query.OrderBy(x => x.TimeMilliseconds).Take(MaxScores).ToList();
        }

        return query.OrderByDescending(x => x.Score).Take(MaxScores).ToList();
    }

    public void SaveScore(string name, int score, string gameMode, TimeSpan? sprintTime = null)
    {
        using TetrisDbContext db = new();

        db.HighScores.Add(
            new HighScoreEntry
            {
                Name = name,
                Score = score,
                GameMode = gameMode,
                TimeMilliseconds = sprintTime.HasValue
                    ? (int)sprintTime.Value.TotalMilliseconds
                    : 0,
            }
        );

        db.SaveChanges();

        List<HighScoreEntry> scores = db.HighScores.Where(x => x.GameMode == gameMode).ToList();

        if (gameMode == GameMode.Sprint.ToString())
        {
            scores = scores.OrderBy(x => x.TimeMilliseconds).ToList();
        }
        else
        {
            scores = scores.OrderByDescending(x => x.Score).ToList();
        }

        if (scores.Count > MaxScores)
        {
            db.HighScores.RemoveRange(scores.Skip(MaxScores));
            db.SaveChanges();
        }
    }

    public int GetPlayerBestScore(string username, string gameMode)
    {
        using TetrisDbContext db = new();

        return db.HighScores.Where(x => x.Name == username && x.GameMode == gameMode)
                .Select(x => (int?)x.Score)
                .Max()
            ?? 0;
    }

    public TimeSpan? GetPlayerBestSprintTime(string username)
    {
        using TetrisDbContext db = new();

        int? bestMilliseconds = db
            .HighScores.Where(x =>
                x.Name == username
                && x.GameMode == GameMode.Sprint.ToString()
                && x.TimeMilliseconds > 0
            )
            .Select(x => (int?)x.TimeMilliseconds)
            .Min();

        return bestMilliseconds.HasValue ? TimeSpan.FromMilliseconds(bestMilliseconds.Value) : null;
    }

    public int GetPlayerScoreCount(string username, string gameMode)
    {
        using TetrisDbContext db = new();

        return db.HighScores.Count(x => x.Name == username && x.GameMode == gameMode);
    }
}
