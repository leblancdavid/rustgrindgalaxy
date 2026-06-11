using Godot;

public partial class Hud : CanvasLayer
{
    private const float TrickPopupSeconds = 2.0f;
    private const float RespawnMessageDuration = 2.0f;

    private Label _missionLabel = null!;
    private Label _statusLabel = null!;
    private Label _healthLabel = null!;
    private Label _materialsLabel = null!;
    private Label _messageLabel = null!;
    private Label _comboLabel = null!;
    private Label _tileLabel = null!;
    private Label _trickPopupLabel = null!;
    private Label _landedComboLabel = null!;
    private float _trickPopupTimeRemaining;
    private float _respawnMessageTimeRemaining;
    private uint _lastSeenTrickSequence;

    public override void _Ready()
    {
        _missionLabel = GetNode<Label>("Margin/VBox/MissionLabel");
        _statusLabel = GetNode<Label>("Margin/VBox/StatusLabel");
        _healthLabel = GetNode<Label>("Margin/VBox/HealthLabel");
        _messageLabel = GetNode<Label>("Margin/VBox/MessageLabel");
        _materialsLabel = GetNode<Label>("Margin/VBox/MaterialsLabel");
        _comboLabel = GetNode<Label>("Margin/VBox/ComboLabel");
        _tileLabel = GetNode<Label>("Margin/VBox/TileLabel");
        _trickPopupLabel = GetNode<Label>("TrickPopup/TrickLabel");
        _landedComboLabel = GetNode<Label>("LandedComboPopup/ComboLabel");
        _trickPopupLabel.Visible = false;
        _landedComboLabel.Visible = false;
        _comboLabel.Visible = false;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;

        if (_trickPopupTimeRemaining > 0.0f)
        {
            _trickPopupTimeRemaining = Mathf.Max(0.0f, _trickPopupTimeRemaining - dt);
            if (_trickPopupTimeRemaining <= 0.0f)
            {
                _trickPopupLabel.Visible = false;
            }
        }

        if (_respawnMessageTimeRemaining > 0.0f)
        {
            _respawnMessageTimeRemaining = Mathf.Max(0.0f, _respawnMessageTimeRemaining - dt);
        }
    }

    public void UpdateTileName(string text)
    {
        _tileLabel.Text = text;
    }

    public void ShowRespawnMessage()
    {
        _respawnMessageTimeRemaining = RespawnMessageDuration;
    }

    public void UpdatePlayerState(PlayerController player)
    {
        var world = GetParentOrNull<World>();
        var state = player.IsGrinding ? "GRIND" : player.IsNearRail ? "NEAR RAIL" : player.IsOnFloor() ? "GROUND" : "AIR";
        var missionTitle = world?.GetMissionTitle() ?? "Industrial Test Run";
        var themeLabel = world?.GetMissionThemeLabel() ?? "Industrial";
        _missionLabel.Text = $"{missionTitle}\n{themeLabel}  T{world?.GetMissionDifficulty() ?? 1}";
        _statusLabel.Text = state;

        if (player.TrickStartSequence != _lastSeenTrickSequence && string.IsNullOrWhiteSpace(player.LastStartedTrickName) == false)
        {
            _lastSeenTrickSequence = player.TrickStartSequence;
            _trickPopupLabel.Text = player.LastStartedTrickName.ToUpperInvariant();
            _trickPopupLabel.Visible = true;
            _trickPopupTimeRemaining = TrickPopupSeconds;
        }

        _comboLabel.Visible = string.IsNullOrWhiteSpace(player.CurrentComboSummary) == false;
        _comboLabel.Text = _comboLabel.Visible
            ? $"COMBO {player.CurrentComboSummary.ToUpperInvariant()}"
            : string.Empty;

        _landedComboLabel.Visible = player.LandedComboDisplayTimeRemaining > 0.0f && string.IsNullOrWhiteSpace(player.LastLandedComboSummary) == false;
        _landedComboLabel.Text = _landedComboLabel.Visible
            ? $"LANDED {player.LastLandedComboSummary.ToUpperInvariant()}"
            : string.Empty;

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

        if (_respawnMessageTimeRemaining > 0.0f)
        {
            _messageLabel.Text = "Respawned";
        }
    }
}
