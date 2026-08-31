using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tetris;

public partial class MainWindow : Window
{
    private const int GridWidth = 10;
    private const int GridHeight = 20;

    public MainWindow()
    {
        InitializeComponent();
        CreateGameBoard();
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

                GameBoard.Children.Add(cell);
            }
        }
    }
}