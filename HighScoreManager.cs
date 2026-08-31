using System.Text.Json;

namespace Tetris;

public class HighScoreEntry
{
    public string Name { get; set; } = "";
    public int Score { get; set; }
}

public class HighScoreManager
{
    private const int MaxScores = 10;

    private readonly string filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Tetris",
        "highscores.json");

    public List<HighScoreEntry> LoadScores()
    {
        if (!File.Exists(filePath))
        {
            return new List<HighScoreEntry>();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<HighScoreEntry>>(json)
                   ?? new List<HighScoreEntry>();
        }
        catch
        {
            return new List<HighScoreEntry>();
        }
    }

    public void SaveScore(string name, int score)
    {
        List<HighScoreEntry> scores = LoadScores();

        scores.Add(new HighScoreEntry
        {
            Name = name,
            Score = score
        });

        scores = scores
            .OrderByDescending(x => x.Score)
            .Take(MaxScores)
            .ToList();

        string? directory = Path.GetDirectoryName(filePath);

        if (directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(scores, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(filePath, json);
    }
}