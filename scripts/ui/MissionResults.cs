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
			GetViewport()?.SetInputAsHandled();
			ReturnToTerminal();
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
			$"{result.ThemeLabel} | T{result.DifficultyTier} | Hazard {GetHazardPressureText(result)}";

		_detailsLabel.Text = result.Succeeded
			? $"Recovered: {result.SummaryText}\nMods: {result.ModifierSummary}"
			: $"Run lost\nMods: {result.ModifierSummary}";

		_totalsLabel.Text =
			$"Stored: {_gameState.GetTotalRecoveredMinerals()}\n" +
			"Jump to return";
	}

	private void ReturnToTerminal()
	{
		_gameState.ClearLastMissionResult();
		var terminalScene = GD.Load<PackedScene>("res://scenes/game/MissionTerminal.tscn");
		GetTree().ChangeSceneToPacked(terminalScene);
	}

	private static string GetHazardPressureText(MissionResultData result)
	{
		return EnvironmentCatalog.GetHazardPressureText(ParseTheme(result.ThemeLabel), result.DifficultyTier);
	}

	private static EnvironmentTheme ParseTheme(string themeLabel)
	{
		return themeLabel switch
		{
			"Industrial" => EnvironmentTheme.Industrial,
			"Rocky" => EnvironmentTheme.Rocky,
			"Frozen" => EnvironmentTheme.Frozen,
			"Derelict" => EnvironmentTheme.Derelict,
			_ => EnvironmentTheme.Industrial,
		};
	}
}
