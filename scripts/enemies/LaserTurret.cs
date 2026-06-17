using Godot;

public partial class LaserTurret : EnemyBase
{
    [Export] public float RotationSpeed { get; set; } = 180.0f;
    [Export] public float AimTelegraphTime { get; set; } = 0.5f;
    [Export] public float FireDuration { get; set; } = 0.3f;
    [Export] public float CooldownDuration { get; set; } = 2.0f;
    [Export] public int LaserDamage { get; set; } = 1;
    [Export] public float DetectionAngleDegrees { get; set; } = 90.0f;
    [Export] public float PivotOffsetX { get; set; } = 0.0f;
    [Export] public float PivotOffsetY { get; set; } = 0.0f;

    private Node2D? _pivot;
    private Node2D? _barrelEnd;
    private Polygon2D? _telegraphLine;
    private float _telegraphTimer;
    private PackedScene? _laserScene;
    private float _cooldownTimer;

    public override void _Ready()
    {
        base._Ready();
        _pivot = GetNodeOrNull<Node2D>("Pivot");
        _barrelEnd = GetNodeOrNull<Node2D>("Pivot/BarrelEnd");
        _telegraphLine = GetNodeOrNull<Polygon2D>("Pivot/TelegraphLine");
        _laserScene = GD.Load<PackedScene>("res://scenes/projectiles/LaserBeam.tscn");
        _cooldownTimer = CooldownDuration * 0.5f;

        if (_telegraphLine != null)
            _telegraphLine.Visible = false;
    }

    protected override void UpdatePatrolState(float delta)
    {
        if (Player == null || Player.IsDead) return;

        RotateTowardPlayer(delta);

        var angleDiff = Mathf.Abs(Mathf.RadToDeg(AngleToPlayer()));
        if (angleDiff <= 5.0f)
        {
            SetState(EnemyState.Alert);
        }
    }

    protected override void UpdateAlertState(float delta)
    {
        RotateTowardPlayer(delta);
        _telegraphTimer += delta;

        if (_telegraphLine != null)
        {
            _telegraphLine.Visible = true;
            var alpha = Mathf.Clamp(_telegraphTimer / AimTelegraphTime, 0.0f, 1.0f);
            _telegraphLine.Color = new Color(1, 0.2f, 0.1f, alpha);
        }

        if (_telegraphTimer >= AimTelegraphTime)
        {
            FireLaser();
            SetState(EnemyState.Attack);
        }
    }

    protected override void UpdateAttackState(float delta)
    {
        _cooldownTimer -= delta;

        if (_telegraphLine != null)
            _telegraphLine.Visible = false;

        if (_cooldownTimer <= 0)
        {
            _cooldownTimer = CooldownDuration;
            _telegraphTimer = 0;
            SetState(EnemyState.Patrol);
        }
    }

    protected override void CheckTransitions()
    {
        if (Player == null || Player.IsDead) return;

        var distance = GlobalPosition.DistanceTo(Player.GlobalPosition);

        switch (CurrentState)
        {
            case EnemyState.Patrol:
                if (DetectionRange > 0 && distance <= DetectionRange)
                {
                    var angleDiff = Mathf.Abs(Mathf.RadToDeg(AngleToPlayer()));
                    if (angleDiff <= DetectionAngleDegrees * 0.5f)
                        SetState(EnemyState.Alert);
                }
                break;
            case EnemyState.Alert:
                if (distance > DetectionRange * 1.2f)
                    SetState(EnemyState.Patrol);
                break;
            case EnemyState.Attack:
                if (distance > DetectionRange * 1.5f)
                {
                    _cooldownTimer = 0;
                    SetState(EnemyState.Patrol);
                }
                break;
        }
    }

    private void RotateTowardPlayer(float delta)
    {
        if (_pivot == null || Player == null) return;

        var targetAngle = AngleToPlayer();
        var currentRotation = _pivot.Rotation;
        var maxStep = Mathf.DegToRad(RotationSpeed) * delta;
        _pivot.Rotation = Mathf.MoveToward(currentRotation, targetAngle, maxStep);
    }

    private float AngleToPlayer()
    {
        if (Player == null) return 0;
        var pivotGlobal = _pivot?.GlobalPosition ?? GlobalPosition;
        return (Player.GlobalPosition - pivotGlobal).Angle();
    }

    private void FireLaser()
    {
        if (_laserScene == null) return;
        if (_barrelEnd == null) return;

        var laser = _laserScene.Instantiate<LaserBeam>();
        GetParent().AddChild(laser);
        laser.Initialize(_barrelEnd.GlobalPosition, Vector2.Right.Rotated(_pivot?.Rotation ?? 0), FireDuration);
    }
}
