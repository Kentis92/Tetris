namespace Tetris.Models;

public class HighScoreEntry
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Score { get; set; }
    public string GameMode { get; set; } = "";
    public int TimeMilliseconds { get; set; }
}
