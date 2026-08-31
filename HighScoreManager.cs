using System.IO;

namespace Tetris;

public class HighScoreManager
{
    private readonly string filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Tetris",
        "highscore.txt");

    public int LoadHighScore()
    {
        if (!File.Exists(filePath))
        {
            return 0;
        }

        string savedScore = File.ReadAllText(filePath);

        return int.TryParse(savedScore, out int highScore) ? highScore : 0;
    }

    public void SaveHighScore(int score)
    {
        string? directory = Path.GetDirectoryName(filePath);

        if (directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, score.ToString());
    }
}