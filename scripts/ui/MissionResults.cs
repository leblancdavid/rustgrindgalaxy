using Godot;

public partial class MissionResults : Control
{
    private GameState _gameState = null!;
    private Label _titleLabel = null!;
    private Label _summaryLabel = null!;
    private Label _detailsLabel = null!;
    private Label _totalsLabel = null!;

    public override void _Ready()
    {
        _gameState = GetNode<GameState>("/root/GameState");
        _titleLabel = GetNode<Label>("Panel/Margin/VBox/TitleLabel");
        _summaryLabel = GetNode<Label>("Panel/Margin/VBox/SummaryLabel");
        _detailsLabel = GetNode<Label>("Panel/Margin/VBox/DetailsLabel");
        _totalsLabel = GetNode<Label>("Panel/Margin/VBox/TotalsLabel");

        UpdateView();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept"))
        {
            ReturnToTerminal();
            GetViewport().SetInputAsHandled();
        }
    }

    private void UpdateView()
    {
        var result = _gameState.LastMissionResult;
        if (result == null)
        {
            _titleLabel.Text = "No Mission Result";
            _summaryLabel.Text = "No mission result is available.";
            _detailsLabel.Text = "Press Jump to return to the terminal.";
            _totalsLabel.Text = string.Empty;
            return;
        }

        _titleLabel.Text = result.Succeeded ? "Mission Complete" : "Mission Failed";
        _summaryLabel.Text =
            $"{result.MissionTitle}\n" +
            $"Theme: {result.ThemeLabel}  Difficulty: T{result.DifficultyTier}  Hazard Pressure: {GetHazardPressureText(result.ThemeLabel, result.DifficultyTier)}";

        _detailsLabel.Text = result.Succeeded
            ? $"Collected {result.TotalCollected} / {result.MaterialTarget} required minerals.\nRecovered: {result.SummaryText}\nModifiers: {result.ModifierSummary}"
            : $"The run ended before extraction.\nRecovered this run: {result.SummaryText}\nModifiers: {result.ModifierSummary}";

        _totalsLabel.Text =
            $"Stored Minerals: {_gameState.GetTotalRecoveredMinerals()}\n" +
            $"Campaign Summary: {_gameState.GetRecoveredMineralSummary()}\n\n" +
            "Press Jump to return to the terminal.";
    }

    private void ReturnToTerminal()
    {
        _gameState.ClearLastMissionResult();
        var terminalScene = GD.Load<PackedScene>("res://scenes/game/MissionTerminal.tscn");
        GetTree().ChangeSceneToPacked(terminalScene);
    }

    private static string GetHazardPressureText(string themeLabel, int difficultyTier)
    {
        var score = difficultyTier + (themeLabel is "Derelict" or "Frozen" ? 1 : 0);
        return score switch
        {
            <= 2 => "Low",
            <= 4 => "Moderate",
            <= 6 => "High",
            _ => "Severe",
        };
    }
}
