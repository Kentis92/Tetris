using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Tetris.Models;

namespace Tetris;

public partial class MainWindow : Window
{
    private const int GridWidth = 10;
    private const int GridHeight = 20;

    private readonly Border[,] cells = new Border[GridWidth, GridHeight];
    private readonly int[,] grid = new int[GridWidth, GridHeight];
    private readonly TetrominoType[,] gridColors = new TetrominoType[GridWidth, GridHeight];
    private readonly DispatcherTimer gameTimer;
    private readonly Random random = new();
    private readonly HighScoreManager highScoreManager = new();

    private TetrisPiece currentPiece = null!;
    private TetrisPiece nextPiece = null!;
    private int score;
    private bool gameOver;

    public MainWindow()
    {
        InitializeComponent();

        CreateGameBoard();
        CreateNextPiecePreview();

        gameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        gameTimer.Tick += GameTimer_Tick;

        KeyDown += MainWindow_KeyDown;

        ShowMainMenu();
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        ShowGame();
        StartNewGame();
        gameTimer.Start();
        Focus();
    }

    private void OptionsButton_Click(object sender, RoutedEventArgs e)
    {
        MainMenuScreen.Visibility = Visibility.Collapsed;
        OptionsScreen.Visibility = Visibility.Visible;
    }

    private void HighScoresButton_Click(object sender, RoutedEventArgs e)
    {
        MainMenuScreen.Visibility = Visibility.Collapsed;
        HighScoresScreen.Visibility = Visibility.Visible;
        DisplayHighScores();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMainMenu();
    }

    private void MainMenuButton_Click(object sender, RoutedEventArgs e)
    {
        gameTimer.Stop();
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        MainMenuScreen.Visibility = Visibility.Visible;
        GameScreen.Visibility = Visibility.Collapsed;
        OptionsScreen.Visibility = Visibility.Collapsed;
        HighScoresScreen.Visibility = Visibility.Collapsed;
        GameOverScreen.Visibility = Visibility.Collapsed;
    }

    private void ShowGame()
    {
        MainMenuScreen.Visibility = Visibility.Collapsed;
        GameScreen.Visibility = Visibility.Visible;
        OptionsScreen.Visibility = Visibility.Collapsed;
        HighScoresScreen.Visibility = Visibility.Collapsed;
        GameOverScreen.Visibility = Visibility.Collapsed;
    }

    private void StartNewGame()
    {
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                grid[x, y] = 0;
                gridColors[x, y] = default;
            }
        }

        score = 0;
        gameOver = false;

        GameOverScreen.Visibility = Visibility.Collapsed;

        currentPiece = CreateRandomPiece();
        nextPiece = CreateRandomPiece();

        UpdateScore();
        DrawBoard();
        DrawNextPiece();
    }

    private void CreateGameBoard()
    {
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                Border cell = new Border
                {
                    Background = Brushes.Black,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(1),
                    CornerRadius = new CornerRadius(2)
                };

                cells[x, y] = cell;
                GameBoard.Children.Add(cell);
            }
        }
    }

    private void CreateNextPiecePreview()
    {
        for (int i = 0; i < 16; i++)
        {
            Border cell = new Border
            {
                Background = Brushes.Black,
                Margin = new Thickness(1),
                CornerRadius = new CornerRadius(2)
            };

            NextPiecePreview.Children.Add(cell);
        }
    }

    private void GameTimer_Tick(object? sender, EventArgs e)
    {
        if (gameOver)
        {
            return;
        }

        if (CanMove(0, 1))
        {
            currentPiece.Y++;
        }
        else
        {
            LockPiece();
            ClearCompletedLines();

            if (!SpawnNewPiece())
            {
                EndGame();
                return;
            }
        }

        DrawBoard();
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (gameOver)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Left:
                if (CanMove(-1, 0))
                {
                    currentPiece.X--;
                }
                break;

            case Key.Right:
                if (CanMove(1, 0))
                {
                    currentPiece.X++;
                }
                break;

            case Key.Down:
                if (CanMove(0, 1))
                {
                    currentPiece.Y++;
                }
                break;

            case Key.Up:
                TryRotate();
                break;

            case Key.Space:
                while (CanMove(0, 1))
                {
                    currentPiece.Y++;
                }

                LockPiece();
                ClearCompletedLines();

                if (!SpawnNewPiece())
                {
                    EndGame();
                    return;
                }
                break;
        }

        DrawBoard();
    }

    private bool CanMove(int moveX, int moveY)
    {
        for (int y = 0; y < currentPiece.Shape.GetLength(0); y++)
        {
            for (int x = 0; x < currentPiece.Shape.GetLength(1); x++)
            {
                if (currentPiece.Shape[y, x] == 0)
                {
                    continue;
                }

                int newX = currentPiece.X + x + moveX;
                int newY = currentPiece.Y + y + moveY;

                if (newX < 0 || newX >= GridWidth)
                {
                    return false;
                }

                if (newY >= GridHeight)
                {
                    return false;
                }

                if (newY >= 0 && grid[newX, newY] == 1)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void TryRotate()
    {
        int[,] originalShape = currentPiece.Shape;

        currentPiece.Rotate();

        if (!CanMove(0, 0))
        {
            currentPiece.Shape = originalShape;
        }
    }

    private void LockPiece()
    {
        for (int y = 0; y < currentPiece.Shape.GetLength(0); y++)
        {
            for (int x = 0; x < currentPiece.Shape.GetLength(1); x++)
            {
                if (currentPiece.Shape[y, x] == 1)
                {
                    int boardX = currentPiece.X + x;
                    int boardY = currentPiece.Y + y;

                    if (boardX >= 0 && boardX < GridWidth &&
                        boardY >= 0 && boardY < GridHeight)
                    {
                        grid[boardX, boardY] = 1;
                        gridColors[boardX, boardY] = currentPiece.Type;
                    }
                }
            }
        }
    }

    private void ClearCompletedLines()
    {
        int linesCleared = 0;

        for (int y = GridHeight - 1; y >= 0; y--)
        {
            if (IsLineFull(y))
            {
                RemoveLine(y);
                linesCleared++;
                y++;
            }
        }

        AddScore(linesCleared);
    }

    private bool IsLineFull(int y)
    {
        for (int x = 0; x < GridWidth; x++)
        {
            if (grid[x, y] == 0)
            {
                return false;
            }
        }

        return true;
    }

    private void RemoveLine(int line)
    {
        for (int y = line; y > 0; y--)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                grid[x, y] = grid[x, y - 1];
                gridColors[x, y] = gridColors[x, y - 1];
            }
        }

        for (int x = 0; x < GridWidth; x++)
        {
            grid[x, 0] = 0;
            gridColors[x, 0] = default;
        }
    }

    private void AddScore(int linesCleared)
    {
        score += linesCleared switch
        {
            1 => 100,
            2 => 300,
            3 => 500,
            4 => 800,
            _ => 0
        };

        UpdateScore();
    }

    private void UpdateScore()
    {
        ScoreText.Text = $"Score: {score}";
    }

    private bool SpawnNewPiece()
    {
        currentPiece = nextPiece;
        nextPiece = CreateRandomPiece();

        DrawNextPiece();

        return CanMove(0, 0);
    }

    private TetrisPiece CreateRandomPiece()
    {
        TetrominoType type = (TetrominoType)random.Next(7);
        return new TetrisPiece(type);
    }

    private void EndGame()
    {
        gameOver = true;
        gameTimer.Stop();

        FinalScoreText.Text = $"Score: {score}";
        PlayerNameTextBox.Text = "";
        GameOverScreen.Visibility = Visibility.Visible;
        PlayerNameTextBox.Focus();
    }

    private void SaveScoreButton_Click(object sender, RoutedEventArgs e)
    {
        string name = PlayerNameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Please enter your name.");
            PlayerNameTextBox.Focus();
            return;
        }

        highScoreManager.SaveScore(name, score);
        DisplayHighScores();

        GameOverScreen.Visibility = Visibility.Collapsed;
        HighScoresScreen.Visibility = Visibility.Visible;
    }

    private void DisplayHighScores()
    {
        HighScoresList.Children.Clear();

        List<HighScoreEntry> scores = highScoreManager.LoadScores();

        if (scores.Count == 0)
        {
            HighScoresList.Children.Add(new TextBlock
            {
                Text = "No scores yet.",
                Foreground = Brushes.Gray,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            return;
        }

        for (int i = 0; i < scores.Count; i++)
        {
            HighScoreEntry entry = scores[i];

            HighScoresList.Children.Add(new TextBlock
            {
                Text = $"{i + 1}. {entry.Name} - {entry.Score}",
                Foreground = Brushes.White,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 2)
            });
        }
    }

    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        StartNewGame();
        gameTimer.Start();
        Focus();
    }

    private Brush GetPieceColor(TetrominoType type)
    {
        return type switch
        {
            TetrominoType.I => Brushes.Cyan,
            TetrominoType.O => Brushes.Yellow,
            TetrominoType.T => Brushes.Purple,
            TetrominoType.S => Brushes.Green,
            TetrominoType.Z => Brushes.Red,
            TetrominoType.J => Brushes.Blue,
            TetrominoType.L => Brushes.Orange,
            _ => Brushes.White
        };
    }

    private void DrawBoard()
    {
        ClearBoard();

        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                if (grid[x, y] == 1)
                {
                    cells[x, y].Background = GetPieceColor(gridColors[x, y]);
                }
            }
        }

        DrawPiece();
    }

    private void DrawPiece()
    {
        Brush color = GetPieceColor(currentPiece.Type);

        for (int y = 0; y < currentPiece.Shape.GetLength(0); y++)
        {
            for (int x = 0; x < currentPiece.Shape.GetLength(1); x++)
            {
                if (currentPiece.Shape[y, x] == 1)
                {
                    int boardX = currentPiece.X + x;
                    int boardY = currentPiece.Y + y;

                    if (boardX >= 0 && boardX < GridWidth &&
                        boardY >= 0 && boardY < GridHeight)
                    {
                        cells[boardX, boardY].Background = color;
                    }
                }
            }
        }
    }

    private void DrawNextPiece()
    {
        ClearNextPiecePreview();

        Brush color = GetPieceColor(nextPiece.Type);

        int offsetX = (4 - nextPiece.Shape.GetLength(1)) / 2;
        int offsetY = (4 - nextPiece.Shape.GetLength(0)) / 2;

        for (int y = 0; y < nextPiece.Shape.GetLength(0); y++)
        {
            for (int x = 0; x < nextPiece.Shape.GetLength(1); x++)
            {
                if (nextPiece.Shape[y, x] == 1)
                {
                    int previewX = offsetX + x;
                    int previewY = offsetY + y;

                    int index = previewY * 4 + previewX;

                    if (index >= 0 && index < NextPiecePreview.Children.Count)
                    {
                        Border cell = (Border)NextPiecePreview.Children[index];
                        cell.Background = color;
                    }
                }
            }
        }
    }

    private void ClearNextPiecePreview()
    {
        foreach (Border cell in NextPiecePreview.Children)
        {
            cell.Background = Brushes.Black;
        }
    }

    private void ClearBoard()
    {
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                cells[x, y].Background = Brushes.Black;
            }
        }
    }
}