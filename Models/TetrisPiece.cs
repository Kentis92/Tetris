namespace Tetris.Models;

public class TetrisPiece
{
    public int[,] Shape { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    public TetrisPiece()
    {
        Shape = new int[,]
        {
            { 1, 1, 1, 1 }
        };

        X = 3;
        Y = 0;
    }

    public void Rotate()
    {
        int rows = Shape.GetLength(0);
        int columns = Shape.GetLength(1);

        int[,] rotated = new int[columns, rows];

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                rotated[x, rows - 1 - y] = Shape[y, x];
            }
        }

        Shape = rotated;
    }
}