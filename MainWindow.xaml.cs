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

    private TetrisPiece currentPiece = null!;
    private int score;
    private bool gameOver;

    public MainWindow()
    {
        InitializeComponent();

        CreateGameBoard();

        gameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        gameTimer.Tick += GameTimer_Tick;

        KeyDown += MainWindow_KeyDown;

        Focus();

        StartNewGame();

        gameTimer.Start();
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

        UpdateScore();
        DrawBoard();
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
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1)
                };

                cells[x, y] = cell;
                GameBoard.Children.Add(cell);
            }
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
        currentPiece = CreateRandomPiece();

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

        FinalScoreText.Text = $"Total Score: {score}";
        GameOverScreen.Visibility = Visibility.Visible;
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