using Godot;

public partial class Hud : CanvasLayer
{
    private Label _statusLabel = null!;
    private Label _healthLabel = null!;
    private Label _messageLabel = null!;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("StatusLabel");
        _healthLabel = GetNode<Label>("HealthLabel");
        _messageLabel = GetNode<Label>("MessageLabel");
    }

    public void UpdatePlayerState(PlayerController player)
    {
        var world = GetParentOrNull<World>();
        var state = player.IsGrinding ? "GRIND" : player.IsNearRail ? "NEAR RAIL" : "GROUND";
        var railSpeed = player.ResolvedEffects.RailSpeedBonus;
        _statusLabel.Text = $"State: {state}\nRail Bonus: {railSpeed:0.0}";

        var materialTarget = world?.MissionMaterialTarget ?? 0;
        _messageLabel.Text = string.Empty;

        if (HasNode("MaterialsLabel"))
        {
            var materialsLabel = GetNode<Label>("MaterialsLabel");
            materialsLabel.Text = world != null
                ? $"Materials: {world.GetTotalCollectedMinerals()} / {materialTarget}\n{world.GetCollectedMineralSummary()}"
                : "Materials: 0";
        }

        if (world != null && world.IsMissionComplete())
        {
            _healthLabel.Text = $"HP: {player.CurrentHealth} / {player.MaxHealth}";
            _messageLabel.Text = "Mission Complete\nPress Jump to Replay";
            return;
        }

        if (player.IsDead)
        {
            _healthLabel.Text = "HP: 0 / 0\nSTATUS: DESTROYED";

            _messageLabel.Text = world != null && world.IsRestartReady()
                ? "Press Jump to Restart"
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
