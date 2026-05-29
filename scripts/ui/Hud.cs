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
        var missionTitle = world?.GetMissionTitle() ?? "Industrial Test Run";
        var themeLabel = world?.GetMissionThemeLabel() ?? "Industrial";
        _missionLabel.Text = $"{missionTitle}\n{themeLabel}  T{world?.GetMissionDifficulty() ?? 1}";
        _statusLabel.Text = state;

        var materialTarget = world?.MissionMaterialTarget ?? 0;
        _messageLabel.Text = string.Empty;

        _materialsLabel.Text = world != null
            ? $"Mat {world.GetTotalCollectedMinerals()} / {materialTarget}"
            : "Materials: 0";

        if (world != null && world.IsMissionComplete())
        {
            _healthLabel.Text = $"HP {player.CurrentHealth}/{player.MaxHealth}";
            _messageLabel.Text = "Clear - Jump";
            return;
        }

        if (player.IsDead)
        {
            _healthLabel.Text = "HP 0";

            _messageLabel.Text = world != null && world.IsRestartReady()
                ? "Destroyed - Jump"
                : "Destroyed";

            return;
        }

        var invuln = player.InvulnerabilityTimeRemaining > 0.0f ? " *" : string.Empty;
        _healthLabel.Text = $"HP {player.CurrentHealth}/{player.MaxHealth}{invuln}";
        _messageLabel.Text = world != null && world.CanExtract()
            ? "Extract Ready"
            : "Collect";
    }
}
