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
    private readonly DispatcherTimer gameTimer;
    private TetrisPiece currentPiece;

    public MainWindow()
    {
        InitializeComponent();

        CreateGameBoard();

        currentPiece = new TetrisPiece();

        DrawPiece();

        gameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        gameTimer.Tick += GameTimer_Tick;
        gameTimer.Start();

        KeyDown += MainWindow_KeyDown;
        Focus();
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
        currentPiece.Y++;

        DrawBoard();
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                currentPiece.X--;
                break;

            case Key.Right:
                currentPiece.X++;
                break;

            case Key.Down:
                currentPiece.Y++;
                break;

            case Key.Up:
                currentPiece.Rotate();
                break;
        }

        DrawBoard();
    }

    private void DrawBoard()
    {
        ClearBoard();
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