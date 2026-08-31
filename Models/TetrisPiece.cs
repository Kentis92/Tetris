namespace Tetris.Models;

public enum TetrominoType
{
    I,
    O,
    T,
    S,
    Z,
    J,
    L
}

public class TetrisPiece
{
    public int[,] Shape { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    public TetrominoType Type { get; }

    public TetrisPiece(TetrominoType type)
    {
        Type = type;

        Shape = type switch
        {
            TetrominoType.I => new int[,]
            {
                { 1, 1, 1, 1 }
            },

            TetrominoType.O => new int[,]
            {
                { 1, 1 },
                { 1, 1 }
            },

            TetrominoType.T => new int[,]
            {
                { 0, 1, 0 },
                { 1, 1, 1 }
            },

            TetrominoType.S => new int[,]
            {
                { 0, 1, 1 },
                { 1, 1, 0 }
            },

            TetrominoType.Z => new int[,]
            {
                { 1, 1, 0 },
                { 0, 1, 1 }
            },

            TetrominoType.J => new int[,]
            {
                { 1, 0, 0 },
                { 1, 1, 1 }
            },

            TetrominoType.L => new int[,]
            {
                { 0, 0, 1 },
                { 1, 1, 1 }
            },

            _ => throw new ArgumentException("Ukjent brikke")
        };

        X = 3;
        Y = 0;
    }

    public void Rotate()
    {
        if (Type == TetrominoType.O)
        {
            return;
        }

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