using Godot;

public partial class Grenadier : EnemyBase
{
    [Export] public float HoverHeight { get; set; } = 100.0f;
    [Export] public float FireRate { get; set; } = 3.0f;
    [Export] public float GrenadeSpeed { get; set; } = 60.0f;
    [Export] public float HoverAmplitude { get; set; } = 4.0f;
    [Export] public float PatrolRange { get; set; } = 40.0f;

    private float _time;
    private float _fireTimer;
    private float _spawnX;
    private float _spawnY;
    private PackedScene? _grenadeScene;
    private Polygon2D? _visual;
    private Color _visualBaseColor;
    private float _tossAnimTimer;

    public override void _Ready()
    {
        base._Ready();
        _spawnX = GlobalPosition.X;
        _spawnY = GlobalPosition.Y;
        _grenadeScene = GD.Load<PackedScene>("res://scenes/projectiles/GrenadeProjectile.tscn");
        _visual = GetNodeOrNull<Polygon2D>("Visual");
        if (_visual != null)
            _visualBaseColor = _visual.Color;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_tossAnimTimer > 0 && _visual != null)
        {
            _tossAnimTimer -= (float)delta;
            var t = 1.0f - (_tossAnimTimer / 0.2f);
            var p = 1.0f - t;
            _visual.Rotation = t * 0.3f * Scale.X;
            _visual.Color = _visualBaseColor.Lerp(new Color(1, 1, 0.6f), p);

            var sq = 1.0f + p * 0.15f;
            _visual.Scale = new Vector2(1.0f / sq, sq);
        }
    }

    protected override void UpdatePatrolState(float delta)
    {
        _time += delta;
        FaceDirection(Mathf.Sin(_time * 0.3f));

        var pos = GlobalPosition;
        pos.X = _spawnX + Mathf.Sin(_time * 0.3f) * PatrolRange;
        pos.Y = _spawnY + Mathf.Sin(_time * 0.5f) * HoverAmplitude;
        GlobalPosition = pos;
        ClampAboveFloor(30f);
    }

    protected override void UpdateChaseState(float delta)
    {
        UpdatePatrolState(delta);

        _fireTimer -= delta;
        if (_fireTimer <= 0)
        {
            _fireTimer = FireRate;
            LobGrenade();
        }
    }

    protected override void UpdateAttackState(float delta)
    {
        _time += delta;
        var pos = GlobalPosition;
        pos.Y = _spawnY + Mathf.Sin(_time * 0.5f) * HoverAmplitude;
        GlobalPosition = pos;
        ClampAboveFloor(30f);

        FacePlayer();
        LobGrenade();

        SetState(EnemyState.Chase);
    }

    protected override void CheckTransitions()
    {
        if (Player == null || Player.IsDead) return;

        var distance = GlobalPosition.DistanceTo(Player.GlobalPosition);

        switch (CurrentState)
        {
            case EnemyState.Patrol:
                if (DetectionRange > 0 && distance <= DetectionRange)
                    SetState(EnemyState.Chase);
                break;
            case EnemyState.Chase:
                if (distance > DetectionRange * 1.5f)
                    SetState(EnemyState.Patrol);
                break;
        }
    }

    private void LobGrenade()
    {
        if (_grenadeScene == null || Player == null) return;

        var grenade = _grenadeScene.Instantiate<GrenadeProjectile>();
        GetParent().AddChild(grenade);

        var targetX = Player.GlobalPosition.X;
        var targetY = Player.GlobalPosition.Y;
        var dir = new Vector2(targetX - GlobalPosition.X, targetY - GlobalPosition.Y).Normalized();

        grenade.Initialize(
            GlobalPosition + new Vector2(0, 8),
            dir * GrenadeSpeed,
            1.5f,
            ContactDamage
        );

        _tossAnimTimer = 0.2f;
    }
}
