using Godot;

public partial class RespawnBeacon : Area2D
{
    private static readonly Color BodyIdleColor = new Color(0.35f, 0.35f, 0.38f);
    private static readonly Color BodyActivatedColor = new Color(0.3f, 0.7f, 1.0f);
    private static readonly Color BeamColor = new Color(0.5f, 0.8f, 1.0f, 0.6f);
    private const float RespawnOffsetY = 30.0f;

    private bool _activated;
    private PlayerController _player = null!;
    private Polygon2D _visual = null!;
    private Polygon2D _beam = null!;
    private Label _label = null!;

    public override void _Ready()
    {
        ZIndex = 10;
        _visual = GetNode<Polygon2D>("Visual");
        _beam = GetNode<Polygon2D>("Beam");
        _label = GetNode<Label>("Label");
        _beam.Visible = false;
    }

    public void SetPlayer(PlayerController player)
    {
        _player = player;
    }

    public override void _Process(double delta)
    {
        if (_activated || _player == null)
            return;

        if (_player.GlobalPosition.X > GlobalPosition.X)
            Activate();
    }

    private void Activate()
    {
        _activated = true;
        _visual.Color = BodyActivatedColor;

        var respawnPos = GlobalPosition - new Vector2(0.0f, RespawnOffsetY);
        var world = _player.GetParentOrNull<World>();
        world?.SetRespawnPoint(respawnPos);

        var procGenTest = _player.GetParentOrNull<ProcGenTest>();
        procGenTest?.SetRespawnPoint(respawnPos);

        PlayBeamEffect();
    }

    private void PlayBeamEffect()
    {
        _beam.Visible = true;
        _beam.Scale = new Vector2(1.0f, 0.0f);
        _beam.Modulate = BeamColor;

        var tween = CreateTween();
        tween.SetParallel(false);
        tween.TweenProperty(_beam, "scale", Vector2.One, 0.5f)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_beam, "modulate", new Color(1.0f, 1.0f, 1.0f, 0.3f), 1.0f);
    }
}
