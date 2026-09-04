using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Tetris.Data;
using Tetris.Models;

namespace Tetris;

public partial class MainWindow : Window
{
    private const int GridWidth = 10;
    private const int GridHeight = 20;

    private const int ClassicDropMilliseconds = 500;
    private const int EndlessStartDropMilliseconds = 650;
    private const int EndlessMinimumDropMilliseconds = 100;
    private const int EndlessSpeedStepMilliseconds = 50;
    private const int EndlessLinesPerLevel = 10;

    private const int SprintTargetLines = 40;
    private const int TimeAttackSeconds = 120;

    private readonly Border[,] cells = new Border[GridWidth, GridHeight];
    private readonly int[,] grid = new int[GridWidth, GridHeight];
    private readonly TetrominoType[,] gridColors = new TetrominoType[GridWidth, GridHeight];
    private readonly DispatcherTimer gameTimer;
    private readonly DispatcherTimer modeTimer;
    private readonly Random random = new();
    private readonly List<TetrominoType> pieceBag = new();
    private readonly HighScoreManager highScoreManager = new();
    private readonly UserManager userManager = new();

    private TetrisPiece currentPiece = null!;
    private TetrisPiece nextPiece = null!;
    private string? currentUsername;
    private int score;
    private int combo;
    private int totalLinesCleared;
    private int timeRemaining;
    private DateTime sprintStartTime;
    private TimeSpan sprintCompletionTime;
    private GameMode selectedGameMode = GameMode.Classic;
    private GameMode highScoreDisplayMode = GameMode.Classic;
    private bool gameOver;
    private bool isPaused;
    private bool isShaking;
    private bool isClearingLines;
    private int scorePopupId;
    private int comboPopupId;

    public MainWindow()
    {
        InitializeComponent();

        using TetrisDbContext db = new();
        db.EnsureDatabaseCreated();

        CreateGameBoard();
        CreateNextPiecePreview();

        gameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(ClassicDropMilliseconds),
        };

        modeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

        gameTimer.Tick += GameTimer_Tick;
        modeTimer.Tick += ModeTimer_Tick;

        KeyDown += MainWindow_KeyDown;

        ShowLogin();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        string username = LoginUsernameTextBox.Text.Trim();
        string password = LoginPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Please enter your username.");
            LoginUsernameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Please enter your password.");
            LoginPasswordBox.SelectAll();
            LoginPasswordBox.Focus();
            return;
        }

        User? user = userManager.Login(username, password);

        if (user == null)
        {
            MessageBox.Show("Invalid username or password.");
            LoginPasswordBox.SelectAll();
            LoginPasswordBox.Focus();
            return;
        }

        currentUsername = user.Username;
        WelcomeText.Text = $"Welcome, {currentUsername}";

        LoginUsernameTextBox.Text = "";
        LoginPasswordBox.Password = "";

        ShowMainMenu();
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        LoginScreen.Visibility = Visibility.Collapsed;
        RegisterScreen.Visibility = Visibility.Visible;

        RegisterUsernameTextBox.Text = "";
        RegisterPasswordBox.Password = "";

        RegisterUsernameTextBox.Focus();
    }

    private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
    {
        string username = RegisterUsernameTextBox.Text.Trim();
        string password = RegisterPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Please enter a username.");
            RegisterUsernameTextBox.Focus();
            return;
        }

        if (username.Length < 3)
        {
            MessageBox.Show("Username must be at least 3 characters.");
            RegisterUsernameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Please enter a password.");
            RegisterPasswordBox.Focus();
            return;
        }

        if (password.Length < 4)
        {
            MessageBox.Show("Password must be at least 4 characters.");
            RegisterPasswordBox.Focus();
            return;
        }

        bool registered;

        try
        {
            registered = userManager.Register(username, password);
        }
        catch (Exception ex)
        {
            Exception? inner = ex.InnerException;
            string details = ex.Message;

            while (inner != null)
            {
                details += $"\n\nINNER EXCEPTION:\n{inner.Message}";
                inner = inner.InnerException;
            }

            MessageBox.Show(
                $"Registration crashed:\n\n{ex.GetType().Name}\n\n{details}",
                "Registration Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            return;
        }

        if (!registered)
        {
            MessageBox.Show("That username already exists.");
            RegisterUsernameTextBox.Focus();
            return;
        }

        MessageBox.Show("Account created successfully. You can now log in.");

        RegisterUsernameTextBox.Text = "";
        RegisterPasswordBox.Password = "";

        ShowLogin();
    }

    private void BackToLoginButton_Click(object sender, RoutedEventArgs e)
    {
        ShowLogin();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        gameTimer.Stop();
        modeTimer.Stop();
        isPaused = false;
        gameOver = false;
        currentUsername = null;

        ShowLogin();
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        gameTimer.Stop();
        modeTimer.Stop();

        MainMenuScreen.Visibility = Visibility.Collapsed;
        GameModeScreen.Visibility = Visibility.Visible;
    }

    private void ClassicModeButton_Click(object sender, RoutedEventArgs e)
    {
        StartGameMode(GameMode.Classic);
    }

    private void EndlessModeButton_Click(object sender, RoutedEventArgs e)
    {
        StartGameMode(GameMode.Endless);
    }

    private void SprintModeButton_Click(object sender, RoutedEventArgs e)
    {
        StartGameMode(GameMode.Sprint);
    }

    private void TimeAttackModeButton_Click(object sender, RoutedEventArgs e)
    {
        StartGameMode(GameMode.TimeAttack);
    }

    private void StartGameMode(GameMode mode)
    {
        selectedGameMode = mode;
        highScoreDisplayMode = mode;

        GameModeScreen.Visibility = Visibility.Collapsed;

        ShowGame();
        StartNewGame();

        gameTimer.Start();

        if (selectedGameMode == GameMode.TimeAttack)
        {
            modeTimer.Start();
        }

        Focus();
    }

    private void GameModeBackButton_Click(object sender, RoutedEventArgs e)
    {
        GameModeScreen.Visibility = Visibility.Collapsed;
        ShowMainMenu();
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

        DisplayHighScores(highScoreDisplayMode);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
    {
        gameTimer.Stop();
        modeTimer.Stop();
        isPaused = false;

        ShowMainMenu();
    }

    private void MainMenuButton_Click(object sender, RoutedEventArgs e)
    {
        gameTimer.Stop();
        modeTimer.Stop();
        isPaused = false;

        ShowMainMenu();
    }

    private void ShowLogin()
    {
        LoginScreen.Visibility = Visibility.Visible;
        MainMenuScreen.Visibility = Visibility.Collapsed;
        GameModeScreen.Visibility = Visibility.Collapsed;
        GameScreen.Visibility = Visibility.Collapsed;
        OptionsScreen.Visibility = Visibility.Collapsed;
        HighScoresScreen.Visibility = Visibility.Collapsed;
        RegisterScreen.Visibility = Visibility.Collapsed;
        GameOverScreen.Visibility = Visibility.Collapsed;
        PauseScreen.Visibility = Visibility.Collapsed;

        HideScorePopup();
        HideComboPopup();

        LoginUsernameTextBox.Focus();
    }

    private void ShowMainMenu()
    {
        LoginScreen.Visibility = Visibility.Collapsed;
        MainMenuScreen.Visibility = Visibility.Visible;
        GameModeScreen.Visibility = Visibility.Collapsed;
        GameScreen.Visibility = Visibility.Collapsed;
        OptionsScreen.Visibility = Visibility.Collapsed;
        HighScoresScreen.Visibility = Visibility.Collapsed;
        RegisterScreen.Visibility = Visibility.Collapsed;
        GameOverScreen.Visibility = Visibility.Collapsed;
        PauseScreen.Visibility = Visibility.Collapsed;

        HideScorePopup();
        HideComboPopup();

        WelcomeText.Text = $"Welcome, {currentUsername}";
    }

    private void ShowGame()
    {
        LoginScreen.Visibility = Visibility.Collapsed;
        MainMenuScreen.Visibility = Visibility.Collapsed;
        GameModeScreen.Visibility = Visibility.Collapsed;
        GameScreen.Visibility = Visibility.Visible;
        OptionsScreen.Visibility = Visibility.Collapsed;
        HighScoresScreen.Visibility = Visibility.Collapsed;
        RegisterScreen.Visibility = Visibility.Collapsed;
        GameOverScreen.Visibility = Visibility.Collapsed;
        PauseScreen.Visibility = Visibility.Collapsed;

        GameScreenTransform.X = 0;
        GameScreenTransform.Y = 0;
    }

    private void StartNewGame()
    {
        gameTimer.Stop();
        modeTimer.Stop();

        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                grid[x, y] = 0;
                gridColors[x, y] = default;
            }
        }

        score = 0;
        combo = 0;
        totalLinesCleared = 0;
        timeRemaining = TimeAttackSeconds;
        sprintStartTime = DateTime.Now;
        sprintCompletionTime = TimeSpan.Zero;
        gameOver = false;
        isPaused = false;
        isClearingLines = false;

        pieceBag.Clear();

        scorePopupId++;
        comboPopupId++;

        HideScorePopup();
        HideComboPopup();

        GameScreenTransform.X = 0;
        GameScreenTransform.Y = 0;

        GameOverScreen.Visibility = Visibility.Collapsed;
        PauseScreen.Visibility = Visibility.Collapsed;

        GamePlayerText.Text = currentUsername ?? "Player";

        UpdateGameSpeed();
        UpdateGameModeText();

        GameOverTitleText.Text = "GAME OVER";

        currentPiece = CreateRandomPiece();
        nextPiece = CreateRandomPiece();

        UpdateScore();
        UpdateModeInfo();
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
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                };

                cells[x, y] = cell;
                GameBoard.Children.Add(cell);
            }
        }
    }

    private void CreateNextPiecePreview()
    {
        for (int i = 0; i < 25; i++)
        {
            Border cell = new Border
            {
                Background = Brushes.Black,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(1),
                CornerRadius = new CornerRadius(2),
            };

            NextPiecePreview.Children.Add(cell);
        }
    }

    private async void GameTimer_Tick(object? sender, EventArgs e)
    {
        if (gameOver || isPaused || isClearingLines)
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

            await ClearCompletedLines();

            if (gameOver)
            {
                return;
            }

            if (!SpawnNewPiece())
            {
                EndGame();
                return;
            }
        }

        DrawBoard();
    }

    private void ModeTimer_Tick(object? sender, EventArgs e)
    {
        if (gameOver || isPaused || isClearingLines)
        {
            return;
        }

        if (selectedGameMode != GameMode.TimeAttack)
        {
            return;
        }

        timeRemaining--;

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            UpdateModeInfo();
            EndTimeAttack();
            return;
        }

        UpdateModeInfo();
    }

    private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (!gameOver && GameScreen.Visibility == Visibility.Visible)
            {
                TogglePause();
            }

            return;
        }

        if (GameScreen.Visibility != Visibility.Visible)
        {
            return;
        }

        if (gameOver || isPaused || isClearingLines)
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
                ShakeScreen();

                await ClearCompletedLines();

                if (gameOver)
                {
                    return;
                }

                if (!SpawnNewPiece())
                {
                    EndGame();
                    return;
                }

                break;

            default:
                return;
        }

        DrawBoard();
    }

    private async void ShakeScreen()
    {
        if (isShaking)
        {
            return;
        }

        isShaking = true;

        GameScreenTransform.X = -3;
        GameScreenTransform.Y = 1;

        await Task.Delay(30);

        GameScreenTransform.X = 3;
        GameScreenTransform.Y = -1;

        await Task.Delay(30);

        GameScreenTransform.X = -2;
        GameScreenTransform.Y = 0;

        await Task.Delay(30);

        GameScreenTransform.X = 2;
        GameScreenTransform.Y = 0;

        await Task.Delay(30);

        GameScreenTransform.X = 0;
        GameScreenTransform.Y = 0;

        isShaking = false;
    }

    private async Task ClearCompletedLines()
    {
        List<int> completedLines = new();

        for (int y = 0; y < GridHeight; y++)
        {
            if (IsLineFull(y))
            {
                completedLines.Add(y);
            }
        }

        if (completedLines.Count == 0)
        {
            combo = 0;
            HideComboPopup();
            return;
        }

        isClearingLines = true;

        foreach (int line in completedLines)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                cells[x, line].Background = Brushes.White;
            }
        }

        await Task.Delay(60);

        foreach (int line in completedLines)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                cells[x, line].Background = GetPieceBrush(gridColors[x, line]);
            }
        }

        await Task.Delay(60);

        foreach (int line in completedLines)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                cells[x, line].Background = Brushes.White;
            }
        }

        await Task.Delay(60);

        completedLines.Sort();
        completedLines.Reverse();

        foreach (int line in completedLines)
        {
            RemoveLine(line);
        }

        AddScore(completedLines.Count);

        isClearingLines = false;

        if (selectedGameMode == GameMode.Sprint && totalLinesCleared >= SprintTargetLines)
        {
            EndSprint();
            return;
        }

        if (combo > 1)
        {
            ShowComboPopup();
        }
    }

    private void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    private void PauseGame()
    {
        if (gameOver || isClearingLines)
        {
            return;
        }

        isPaused = true;
        gameTimer.Stop();
        modeTimer.Stop();
        PauseScreen.Visibility = Visibility.Visible;
    }

    private void ResumeGame()
    {
        if (gameOver)
        {
            return;
        }

        isPaused = false;
        PauseScreen.Visibility = Visibility.Collapsed;
        gameTimer.Start();

        if (selectedGameMode == GameMode.TimeAttack)
        {
            modeTimer.Start();
        }

        Focus();
    }

    private void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        ResumeGame();
    }

    private void PauseMainMenuButton_Click(object sender, RoutedEventArgs e)
    {
        gameTimer.Stop();
        modeTimer.Stop();
        isPaused = false;
        PauseScreen.Visibility = Visibility.Collapsed;

        ShowMainMenu();
    }

    private void PauseExitButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
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

    private int GetGhostY()
    {
        int ghostY = currentPiece.Y;

        while (CanMoveFromPosition(currentPiece.X, ghostY + 1))
        {
            ghostY++;
        }

        return ghostY;
    }

    private bool CanMoveFromPosition(int positionX, int positionY)
    {
        for (int y = 0; y < currentPiece.Shape.GetLength(0); y++)
        {
            for (int x = 0; x < currentPiece.Shape.GetLength(1); x++)
            {
                if (currentPiece.Shape[y, x] == 0)
                {
                    continue;
                }

                int newX = positionX + x;
                int newY = positionY + y;

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
        if (currentPiece.Type == TetrominoType.O)
        {
            return;
        }

        int originalRotation = currentPiece.RotationState;
        int originalX = currentPiece.X;
        int originalY = currentPiece.Y;
        int[,] originalShape = currentPiece.Shape;

        currentPiece.Rotate();

        int newRotation = currentPiece.RotationState;

        if (CanMove(0, 0))
        {
            return;
        }

        (int x, int y)[] kickOffsets = GetSrsKickOffsets(
            currentPiece.Type,
            originalRotation,
            newRotation
        );

        foreach ((int offsetX, int offsetY) in kickOffsets)
        {
            currentPiece.X = originalX + offsetX;
            currentPiece.Y = originalY + offsetY;

            if (CanMove(0, 0))
            {
                return;
            }
        }

        currentPiece.Shape = originalShape;
        currentPiece.X = originalX;
        currentPiece.Y = originalY;
        currentPiece.RotationState = originalRotation;
    }

    private (int x, int y)[] GetSrsKickOffsets(TetrominoType type, int fromRotation, int toRotation)
    {
        if (type == TetrominoType.I)
        {
            return GetIKickOffsets(fromRotation, toRotation);
        }

        return GetJLSTZKickOffsets(fromRotation, toRotation);
    }

    private (int x, int y)[] GetJLSTZKickOffsets(int fromRotation, int toRotation)
    {
        return (fromRotation, toRotation) switch
        {
            (0, 1) => [(0, 0), (-1, 0), (-1, -1), (0, 2), (-1, 2)],
            (1, 0) => [(0, 0), (1, 0), (1, 1), (0, -2), (1, -2)],
            (1, 2) => [(0, 0), (1, 0), (1, 1), (0, -2), (1, -2)],
            (2, 1) => [(0, 0), (-1, 0), (-1, -1), (0, 2), (-1, 2)],
            (2, 3) => [(0, 0), (1, 0), (1, -1), (0, 2), (1, 2)],
            (3, 2) => [(0, 0), (-1, 0), (-1, 1), (0, -2), (-1, -2)],
            (3, 0) => [(0, 0), (-1, 0), (-1, 1), (0, -2), (-1, -2)],
            (0, 3) => [(0, 0), (1, 0), (1, -1), (0, 2), (1, 2)],
            _ => [(0, 0)],
        };
    }

    private (int x, int y)[] GetIKickOffsets(int fromRotation, int toRotation)
    {
        return (fromRotation, toRotation) switch
        {
            (0, 1) => [(0, 0), (-2, 0), (1, 0), (-2, 1), (1, -2)],
            (1, 0) => [(0, 0), (2, 0), (-1, 0), (2, -1), (-1, 2)],
            (1, 2) => [(0, 0), (-1, 0), (2, 0), (-1, -2), (2, 1)],
            (2, 1) => [(0, 0), (1, 0), (-2, 0), (1, 2), (-2, -1)],
            (2, 3) => [(0, 0), (2, 0), (-1, 0), (2, -1), (-1, 2)],
            (3, 2) => [(0, 0), (-2, 0), (1, 0), (-2, 1), (1, -2)],
            (3, 0) => [(0, 0), (1, 0), (-2, 0), (1, 2), (-2, -1)],
            (0, 3) => [(0, 0), (-1, 0), (2, 0), (-1, -2), (2, 1)],
            _ => [(0, 0)],
        };
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

                    if (boardX >= 0 && boardX < GridWidth && boardY >= 0 && boardY < GridHeight)
                    {
                        grid[boardX, boardY] = 1;
                        gridColors[boardX, boardY] = currentPiece.Type;
                    }
                }
            }
        }
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
        int baseScore = linesCleared switch
        {
            1 => 100,
            2 => 300,
            3 => 500,
            4 => 800,
            _ => 0,
        };

        combo++;

        int comboBonus = (combo - 1) * 50;
        int totalPoints = baseScore + comboBonus;

        score += totalPoints;
        totalLinesCleared += linesCleared;

        UpdateScore();
        UpdateModeInfo();

        if (selectedGameMode == GameMode.Endless)
        {
            UpdateGameSpeed();
            UpdateGameModeText();
        }

        ShowScorePopup(totalPoints);
    }

    private void UpdateScore()
    {
        ScoreText.Text = $"Score: {score}";
    }

    private void UpdateModeInfo()
    {
        ModeInfoText.Text = selectedGameMode switch
        {
            GameMode.Sprint =>
                $"LINES: {Math.Min(totalLinesCleared, SprintTargetLines)} / {SprintTargetLines}",
            GameMode.TimeAttack => $"TIME: {TimeSpan.FromSeconds(timeRemaining):mm\\:ss}",
            _ => "",
        };
    }

    private void UpdateGameSpeed()
    {
        if (selectedGameMode == GameMode.Classic)
        {
            gameTimer.Interval = TimeSpan.FromMilliseconds(ClassicDropMilliseconds);
            return;
        }

        if (selectedGameMode == GameMode.Sprint || selectedGameMode == GameMode.TimeAttack)
        {
            gameTimer.Interval = TimeSpan.FromMilliseconds(ClassicDropMilliseconds);
            return;
        }

        int level = GetEndlessLevel();
        int speedReduction = (level - 1) * EndlessSpeedStepMilliseconds;

        int dropMilliseconds = Math.Max(
            EndlessMinimumDropMilliseconds,
            EndlessStartDropMilliseconds - speedReduction
        );

        gameTimer.Interval = TimeSpan.FromMilliseconds(dropMilliseconds);
    }

    private int GetEndlessLevel()
    {
        return (totalLinesCleared / EndlessLinesPerLevel) + 1;
    }

    private void UpdateGameModeText()
    {
        GameModeText.Text = selectedGameMode switch
        {
            GameMode.Endless => $"ENDLESS  LV {GetEndlessLevel()}",
            GameMode.Sprint => "SPRINT",
            GameMode.TimeAttack => "TIME ATTACK",
            _ => "CLASSIC",
        };
    }

    private async void ShowScorePopup(int points)
    {
        int popupId = ++scorePopupId;

        ScorePopupText.Text = $"+{points}";
        ScorePopupText.Visibility = Visibility.Visible;
        ScorePopupText.Opacity = 1;
        ScorePopupScale.ScaleX = 0.65;
        ScorePopupScale.ScaleY = 0.65;

        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(20);

            if (popupId != scorePopupId)
            {
                return;
            }

            double scale = 0.65 + (0.5 * (i + 1) / 3);
            ScorePopupScale.ScaleX = scale;
            ScorePopupScale.ScaleY = scale;
        }

        for (int i = 0; i < 2; i++)
        {
            await Task.Delay(20);

            if (popupId != scorePopupId)
            {
                return;
            }

            double scale = 1.15 - (0.15 * (i + 1) / 2);
            ScorePopupScale.ScaleX = scale;
            ScorePopupScale.ScaleY = scale;
        }

        await Task.Delay(50);

        if (popupId != scorePopupId)
        {
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(20);

            if (popupId != scorePopupId)
            {
                return;
            }

            ScorePopupText.Opacity = 1 - ((i + 1) / 5.0);
        }

        if (popupId == scorePopupId)
        {
            ScorePopupText.Visibility = Visibility.Collapsed;
        }
    }

    private async void ShowComboPopup()
    {
        int popupId = ++comboPopupId;

        ComboText.Text = $"COMBO x{combo}!";
        ComboText.Visibility = Visibility.Visible;
        ComboText.Opacity = 1;
        ComboScale.ScaleX = 0.7;
        ComboScale.ScaleY = 0.7;

        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(20);

            if (popupId != comboPopupId)
            {
                return;
            }

            double scale = 0.7 + (0.45 * (i + 1) / 3);
            ComboScale.ScaleX = scale;
            ComboScale.ScaleY = scale;
        }

        for (int i = 0; i < 2; i++)
        {
            await Task.Delay(20);

            if (popupId != comboPopupId)
            {
                return;
            }

            double scale = 1.15 - (0.15 * (i + 1) / 2);
            ComboScale.ScaleX = scale;
            ComboScale.ScaleY = scale;
        }

        await Task.Delay(100);

        if (popupId != comboPopupId)
        {
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(20);

            if (popupId != comboPopupId)
            {
                return;
            }

            ComboText.Opacity = 1 - ((i + 1) / 5.0);
        }

        if (popupId == comboPopupId)
        {
            ComboText.Visibility = Visibility.Collapsed;
        }
    }

    private void HideScorePopup()
    {
        ScorePopupText.Visibility = Visibility.Collapsed;
        ScorePopupText.Opacity = 0;
        ScorePopupScale.ScaleX = 1;
        ScorePopupScale.ScaleY = 1;
    }

    private void HideComboPopup()
    {
        ComboText.Visibility = Visibility.Collapsed;
        ComboText.Opacity = 0;
        ComboScale.ScaleX = 1;
        ComboScale.ScaleY = 1;
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
        if (pieceBag.Count == 0)
        {
            FillPieceBag();
        }

        TetrominoType type = pieceBag[0];
        pieceBag.RemoveAt(0);

        return new TetrisPiece(type);
    }

    private void FillPieceBag()
    {
        pieceBag.Clear();

        pieceBag.AddRange(Enum.GetValues<TetrominoType>());

        for (int i = pieceBag.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(i + 1);

            (pieceBag[i], pieceBag[randomIndex]) = (pieceBag[randomIndex], pieceBag[i]);
        }
    }

    private void EndGame()
    {
        gameOver = true;

        gameTimer.Stop();
        modeTimer.Stop();

        isPaused = false;

        GameOverTitleText.Text = "GAME OVER";
        FinalScoreText.Text = $"Score: {score}";
        GameOverPlayerText.Text = $"Player: {currentUsername}";

        PauseScreen.Visibility = Visibility.Collapsed;
        GameOverScreen.Visibility = Visibility.Visible;
    }

    private void EndSprint()
    {
        gameOver = true;

        gameTimer.Stop();
        modeTimer.Stop();

        sprintCompletionTime = DateTime.Now - sprintStartTime;

        GameOverTitleText.Text = "SPRINT COMPLETE";
        FinalScoreText.Text = $"TIME: {FormatSprintTime(sprintCompletionTime)}";
        GameOverPlayerText.Text = $"Score: {score}";

        PauseScreen.Visibility = Visibility.Collapsed;
        GameOverScreen.Visibility = Visibility.Visible;
    }

    private void EndTimeAttack()
    {
        gameOver = true;

        gameTimer.Stop();
        modeTimer.Stop();

        GameOverTitleText.Text = "TIME'S UP";
        FinalScoreText.Text = $"Score: {score}";
        GameOverPlayerText.Text = $"Lines: {totalLinesCleared}";

        PauseScreen.Visibility = Visibility.Collapsed;
        GameOverScreen.Visibility = Visibility.Visible;
    }

    private void SaveScoreButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(currentUsername))
            {
                MessageBox.Show("No player is currently logged in.");
                ShowLogin();
                return;
            }

            if (selectedGameMode == GameMode.Sprint)
            {
                highScoreManager.SaveScore(
                    currentUsername,
                    score,
                    selectedGameMode.ToString(),
                    sprintCompletionTime
                );
            }
            else
            {
                highScoreManager.SaveScore(currentUsername, score, selectedGameMode.ToString());
            }

            highScoreDisplayMode = selectedGameMode;

            DisplayHighScores(highScoreDisplayMode);

            GameOverScreen.Visibility = Visibility.Collapsed;
            HighScoresScreen.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Exception? inner = ex.InnerException;

            string details = ex.Message;

            while (inner != null)
            {
                details += $"\n\nINNER EXCEPTION:\n{inner.Message}";
                inner = inner.InnerException;
            }

            MessageBox.Show(
                $"Save Score crashed:\n\n{ex.GetType().Name}\n\n{details}",
                "Save Score Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private void DisplayHighScores(GameMode gameMode)
    {
        highScoreDisplayMode = gameMode;

        HighScoresList.Children.Clear();

        StackPanel modeButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 15),
        };

        foreach (GameMode mode in Enum.GetValues<GameMode>())
        {
            Button button = new Button
            {
                Content = GetGameModeDisplayName(mode),
                Tag = mode,
                Margin = new Thickness(4, 0, 4, 0),
                Padding = new Thickness(14, 8, 14, 8),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background =
                    mode == gameMode
                        ? new SolidColorBrush(Color.FromRgb(184, 155, 74))
                        : new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                BorderBrush =
                    mode == gameMode
                        ? new SolidColorBrush(Color.FromRgb(220, 190, 100))
                        : new SolidColorBrush(Color.FromRgb(90, 90, 90)),
            };

            button.Click += HighScoreModeButton_Click;

            modeButtons.Children.Add(button);
        }

        HighScoresList.Children.Add(modeButtons);

        TextBlock title = new TextBlock
        {
            Text = $"{GetGameModeDisplayName(gameMode).ToUpper()} HIGH SCORES",
            Foreground = Brushes.White,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10),
        };

        HighScoresList.Children.Add(title);

        string modeName = gameMode.ToString();

        List<HighScoreEntry> scores = highScoreManager.LoadScores(modeName);

        int scoreCount = highScoreManager.GetPlayerScoreCount(currentUsername ?? "", modeName);

        if (gameMode == GameMode.Sprint)
        {
            TimeSpan? bestTime = highScoreManager.GetPlayerBestSprintTime(currentUsername ?? "");

            PlayerHighScoreText.Text = bestTime.HasValue
                ? $"YOUR BEST: {FormatSprintTime(bestTime.Value)}    RUNS: {scoreCount}"
                : $"YOUR BEST: --:--.--    RUNS: {scoreCount}";
        }
        else
        {
            int bestScore = highScoreManager.GetPlayerBestScore(currentUsername ?? "", modeName);

            PlayerHighScoreText.Text = $"YOUR BEST: {bestScore}    SCORES: {scoreCount}";
        }

        if (scores.Count == 0)
        {
            HighScoresList.Children.Add(
                new TextBlock
                {
                    Text = "No scores yet.",
                    Foreground = Brushes.Gray,
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            );

            return;
        }

        for (int i = 0; i < scores.Count; i++)
        {
            HighScoreEntry entry = scores[i];

            bool isCurrentPlayer = string.Equals(
                entry.Name,
                currentUsername,
                StringComparison.OrdinalIgnoreCase
            );

            Border row = new Border
            {
                Background = isCurrentPlayer
                    ? new SolidColorBrush(Color.FromArgb(35, 184, 155, 74))
                    : Brushes.Transparent,
                BorderBrush = isCurrentPlayer
                    ? new SolidColorBrush(Color.FromArgb(150, 184, 155, 74))
                    : new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 2, 0, 2),
            };

            Grid rowGrid = new Grid();

            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });

            rowGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            );

            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            TextBlock rankText = new TextBlock
            {
                Text = $"#{i + 1}",
                Foreground = i < 3 ? Brushes.White : Brushes.Gray,
                FontSize = 16,
                FontWeight = i < 3 ? FontWeights.Bold : FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center,
            };

            TextBlock nameText = new TextBlock
            {
                Text = entry.Name,
                Foreground = isCurrentPlayer
                    ? new SolidColorBrush(Color.FromRgb(184, 155, 74))
                    : Brushes.White,
                FontSize = 16,
                FontWeight = isCurrentPlayer ? FontWeights.Bold : FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            StackPanel resultPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            TextBlock primaryResultText = new TextBlock
            {
                Text =
                    gameMode == GameMode.Sprint
                        ? FormatSprintTime(TimeSpan.FromMilliseconds(entry.TimeMilliseconds))
                        : entry.Score.ToString(),
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            resultPanel.Children.Add(primaryResultText);

            if (gameMode == GameMode.Sprint)
            {
                TextBlock secondaryScoreText = new TextBlock
                {
                    Text = $"Score: {entry.Score}",
                    Foreground = Brushes.Gray,
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Right,
                };

                resultPanel.Children.Add(secondaryScoreText);
            }

            Grid.SetColumn(rankText, 0);
            Grid.SetColumn(nameText, 1);
            Grid.SetColumn(resultPanel, 2);

            rowGrid.Children.Add(rankText);
            rowGrid.Children.Add(nameText);
            rowGrid.Children.Add(resultPanel);

            row.Child = rowGrid;

            HighScoresList.Children.Add(row);
        }
    }

    private void HighScoreModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is GameMode mode)
        {
            DisplayHighScores(mode);
        }
    }

    private string GetGameModeDisplayName(GameMode mode)
    {
        return mode switch
        {
            GameMode.Classic => "Classic",
            GameMode.Endless => "Endless",
            GameMode.Sprint => "Sprint",
            GameMode.TimeAttack => "Time Attack",
            _ => mode.ToString(),
        };
    }

    private string FormatSprintTime(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}.{time.Milliseconds / 10:00}";
    }

    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        StartNewGame();
        gameTimer.Start();

        if (selectedGameMode == GameMode.TimeAttack)
        {
            modeTimer.Start();
        }

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
            _ => Brushes.White,
        };
    }

    private Brush GetPieceBrush(TetrominoType type)
    {
        SolidColorBrush baseBrush = (SolidColorBrush)GetPieceColor(type);
        Color baseColor = baseBrush.Color;

        Color lightColor = Color.FromRgb(
            (byte)Math.Min(255, baseColor.R + 55),
            (byte)Math.Min(255, baseColor.G + 55),
            (byte)Math.Min(255, baseColor.B + 55)
        );

        Color darkColor = Color.FromRgb(
            (byte)(baseColor.R * 0.55),
            (byte)(baseColor.G * 0.55),
            (byte)(baseColor.B * 0.55)
        );

        return new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(lightColor, 0),
                new GradientStop(baseColor, 0.45),
                new GradientStop(darkColor, 1),
            },
            new Point(0, 0),
            new Point(1, 1)
        );
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
                    cells[x, y].Background = GetPieceBrush(gridColors[x, y]);
                    cells[x, y].BorderBrush = new SolidColorBrush(
                        Color.FromArgb(130, 255, 255, 255)
                    );
                }
            }
        }

        DrawGhostPiece();
        DrawPiece();
    }

    private void DrawGhostPiece()
    {
        int ghostY = GetGhostY();

        SolidColorBrush baseBrush = (SolidColorBrush)GetPieceColor(currentPiece.Type);

        SolidColorBrush ghostColor = new SolidColorBrush(baseBrush.Color) { Opacity = 0.2 };

        for (int y = 0; y < currentPiece.Shape.GetLength(0); y++)
        {
            for (int x = 0; x < currentPiece.Shape.GetLength(1); x++)
            {
                if (currentPiece.Shape[y, x] == 1)
                {
                    int boardX = currentPiece.X + x;
                    int boardY = ghostY + y;

                    if (boardX >= 0 && boardX < GridWidth && boardY >= 0 && boardY < GridHeight)
                    {
                        cells[boardX, boardY].Background = ghostColor;
                        cells[boardX, boardY].BorderBrush = new SolidColorBrush(
                            Color.FromArgb(80, 255, 255, 255)
                        );
                    }
                }
            }
        }
    }

    private void DrawPiece()
    {
        Brush color = GetPieceBrush(currentPiece.Type);

        for (int y = 0; y < currentPiece.Shape.GetLength(0); y++)
        {
            for (int x = 0; x < currentPiece.Shape.GetLength(1); x++)
            {
                if (currentPiece.Shape[y, x] == 1)
                {
                    int boardX = currentPiece.X + x;
                    int boardY = currentPiece.Y + y;

                    if (boardX >= 0 && boardX < GridWidth && boardY >= 0 && boardY < GridHeight)
                    {
                        cells[boardX, boardY].Background = color;
                        cells[boardX, boardY].BorderBrush = new SolidColorBrush(
                            Color.FromArgb(180, 255, 255, 255)
                        );
                    }
                }
            }
        }
    }

    private void DrawNextPiece()
    {
        ClearNextPiecePreview();

        Brush color = GetPieceBrush(nextPiece.Type);

        int minX = nextPiece.Shape.GetLength(1);
        int maxX = -1;
        int minY = nextPiece.Shape.GetLength(0);
        int maxY = -1;

        for (int y = 0; y < nextPiece.Shape.GetLength(0); y++)
        {
            for (int x = 0; x < nextPiece.Shape.GetLength(1); x++)
            {
                if (nextPiece.Shape[y, x] == 1)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        int pieceWidth = maxX - minX + 1;
        int pieceHeight = maxY - minY + 1;

        int offsetX = (5 - pieceWidth) / 2 - minX;
        int offsetY = (5 - pieceHeight) / 2 - minY;

        for (int y = 0; y < nextPiece.Shape.GetLength(0); y++)
        {
            for (int x = 0; x < nextPiece.Shape.GetLength(1); x++)
            {
                if (nextPiece.Shape[y, x] == 1)
                {
                    int previewX = offsetX + x;
                    int previewY = offsetY + y;

                    int index = previewY * 5 + previewX;

                    if (index >= 0 && index < NextPiecePreview.Children.Count)
                    {
                        Border cell = (Border)NextPiecePreview.Children[index];

                        cell.Background = color;
                        cell.BorderBrush = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
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
            cell.BorderBrush = Brushes.Transparent;
        }
    }

    private void ClearBoard()
    {
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                cells[x, y].Background = Brushes.Black;
                cells[x, y].BorderBrush = Brushes.Transparent;
            }
        }
    }
}
