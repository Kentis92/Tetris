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
    private readonly DispatcherTimer gameTimer;

    private TetrisPiece currentPiece;

    public MainWindow()
    {
        InitializeComponent();

        CreateGameBoard();

        currentPiece = new TetrisPiece();

        gameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        gameTimer.Tick += GameTimer_Tick;

        KeyDown += MainWindow_KeyDown;

        Focus();

        DrawBoard();

        gameTimer.Start();
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
        if (CanMove(0, 1))
        {
            currentPiece.Y++;
        }
        else
        {
            LockPiece();
            SpawnNewPiece();
        }

        DrawBoard();
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
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
                    }
                }
            }
        }
    }

    private void SpawnNewPiece()
    {
        currentPiece = new TetrisPiece();
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
                    cells[x, y].Background = Brushes.Cyan;
                }
            }
        }

        DrawPiece();
    }

    private void DrawPiece()
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
                        cells[boardX, boardY].Background = Brushes.Cyan;
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