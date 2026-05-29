using Godot;

public partial class Hud : CanvasLayer
{
    private Label _missionLabel = null!;
    private Label _statusLabel = null!;
    private Label _healthLabel = null!;
    private Label _materialsLabel = null!;
    private Label _messageLabel = null!;

    public override void _Ready()
    {
        _missionLabel = GetNode<Label>("Margin/VBox/MissionLabel");
        _statusLabel = GetNode<Label>("Margin/VBox/StatusLabel");
        _healthLabel = GetNode<Label>("Margin/VBox/HealthLabel");
        _messageLabel = GetNode<Label>("Margin/VBox/MessageLabel");
        _materialsLabel = GetNode<Label>("Margin/VBox/MaterialsLabel");
    }

    public void UpdatePlayerState(PlayerController player)
    {
        var world = GetParentOrNull<World>();
        var state = player.IsGrinding ? "GRIND" : player.IsNearRail ? "NEAR RAIL" : "GROUND";
        var railSpeed = player.ResolvedEffects.RailSpeedBonus;
        var missionTitle = world?.GetMissionTitle() ?? "Industrial Test Run";
        var themeLabel = world?.GetMissionThemeLabel() ?? "Industrial";
        _missionLabel.Text = $"{missionTitle}\nTheme: {themeLabel}  Difficulty: T{world?.GetMissionDifficulty() ?? 1}\nModifiers: {world?.GetMissionModifierSummary() ?? "None"}";
        _statusLabel.Text = $"State: {state}\nRail Bonus: {railSpeed:0.0}  Gravity: {player.GravityScale:0.00}";

        var materialTarget = world?.MissionMaterialTarget ?? 0;
        _messageLabel.Text = string.Empty;

        _materialsLabel.Text = world != null
            ? $"Materials: {world.GetTotalCollectedMinerals()} / {materialTarget}\n{world.GetCollectedMineralSummary()}"
            : "Materials: 0";

        if (world != null && world.IsMissionComplete())
        {
            _healthLabel.Text = $"HP: {player.CurrentHealth} / {player.MaxHealth}";
            _messageLabel.Text = "Mission Complete\nPress Jump to Return";
            return;
        }

        if (player.IsDead)
        {
            _healthLabel.Text = "HP: 0 / 0\nSTATUS: DESTROYED";

            _messageLabel.Text = world != null && world.IsRestartReady()
                ? "Press Jump to Return"
                : "System Failure";

            return;
        }

        var invuln = player.InvulnerabilityTimeRemaining > 0.0f ? "  I-FRAMES" : string.Empty;
        _healthLabel.Text = $"HP: {player.CurrentHealth} / {player.MaxHealth}{invuln}";
        _messageLabel.Text = world != null && world.CanExtract()
            ? "Extraction Ready"
            : "Collect minerals";
    }
}
