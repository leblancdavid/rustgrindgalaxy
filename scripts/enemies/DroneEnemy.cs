using Godot;

public partial class DroneEnemy : EnemyBase
{
    [Export] public float HoverAmplitude { get; set; } = 8.0f;
    [Export] public float HoverSpeed { get; set; } = 2.4f;
    [Export] public float PatrolDistance { get; set; } = 30.0f;
    [Export] public float PatrolSpeed { get; set; } = 24.0f;
    [Export] public float ChaseSpeed { get; set; } = 32.0f;
    [Export] public float AttackRange { get; set; } = 320.0f;
    [Export] public float FireCooldown { get; set; } = 1.5f;

    private float _spawnX;
    private float _spawnY;
    private float _direction = 1.0f;
    private float _time;
    private float _fireTimer;
    private PackedScene? _bulletScene;
    private Polygon2D? _coreVisual;
    private Color _coreBaseColor;
    private float _firePulseTimer;

    public override void _Ready()
    {
        base._Ready();
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        InitializeHoverVariance(rng, HoverAmplitude * 0.5f);
        _spawnX = GlobalPosition.X;
        _spawnY = GlobalPosition.Y;
        _bulletScene = GD.Load<PackedScene>("res://scenes/projectiles/BulletProjectile.tscn");
        _coreVisual = GetNodeOrNull<Polygon2D>("Core");
        if (_coreVisual != null)
            _coreBaseColor = _coreVisual.Color;
    }

    public override void _Process(double delta)
    {
        if (CurrentState == EnemyState.Dead)
            return;

        base._Process(delta);

        // Fire pulse animation
        if (_firePulseTimer > 0 && _coreVisual != null)
        {
            _firePulseTimer -= (float)delta;
            var t = 1.0f - (_firePulseTimer / 0.15f);
            var pulse = 1.0f - t;
            _coreVisual.Color = _coreBaseColor.Lerp(new Color(1, 1, 1), pulse);
            _coreVisual.Scale = new Vector2(1, 1) * (1.0f + pulse * 0.3f);
        }
    }

    protected override void UpdatePatrolState(float delta)
    {
        FaceDirection(_direction);
        _time += delta;

        var minX = _spawnX - PatrolDistance;
        var maxX = _spawnX + PatrolDistance;
        var pos = GlobalPosition;

        if (pos.X <= minX)
            _direction = 1.0f;
        else if (pos.X >= maxX)
            _direction = -1.0f;

        _desiredHorizontalVelocity = new Vector2(_direction * PatrolSpeed, 0f);
        Velocity = new Vector2(_direction * PatrolSpeed, HoverSpeed * Mathf.Cos(_time * HoverSpeed + _hoverPhase) * HoverAmplitude * _hoverAmplitudeScale);
        MoveAndSlide();
        ClampAboveFloor(30f);
    }

    protected override void UpdateChaseState(float delta)
    {
        _time += delta;

        if (Player == null)
        {
            SetState(EnemyState.Patrol);
            return;
        }

        var dir = Mathf.Sign(Player.GlobalPosition.X - GlobalPosition.X);
        FaceDirection(dir);

        var targetY = Player.GlobalPosition.Y - 24.0f + _hoverYOffset;
        targetY = Mathf.Clamp(targetY, _spawnY - HoverAmplitude * _hoverAmplitudeScale, _spawnY + HoverAmplitude * _hoverAmplitudeScale);
        var yVelocity = (targetY - GlobalPosition.Y) * 2.0f
                        + HoverSpeed * Mathf.Cos(_time * HoverSpeed + _hoverPhase) * (HoverAmplitude * 0.3f * _hoverAmplitudeScale);

        _desiredHorizontalVelocity = new Vector2(dir * ChaseSpeed, yVelocity);
        Velocity = new Vector2(dir * ChaseSpeed, yVelocity);
        MoveAndSlide();
        ClampAboveFloor(30f);

        _fireTimer -= delta;
        if (_fireTimer <= 0 && GlobalPosition.DistanceTo(Player.GlobalPosition) <= AttackRange)
        {
            _fireTimer = FireCooldown;
            FireBullet();
        }
    }

    protected override void UpdateAttackState(float delta)
    {
        FacePlayer();

        _time += delta;
        var strafeDir = Player != null ? Mathf.Sign(Player.GlobalPosition.X - GlobalPosition.X) : 0f;
        Velocity = new Vector2(
            strafeDir * ChaseSpeed * 0.4f,
            HoverSpeed * Mathf.Cos(_time * HoverSpeed + _hoverPhase) * HoverAmplitude * _hoverAmplitudeScale);
        MoveAndSlide();
        ClampAboveFloor(30f);

        _fireTimer -= delta;
        if (_fireTimer <= 0)
        {
            _fireTimer = FireCooldown;
            FireBullet();
        }
    }

    protected override void CheckTransitions()
    {
        if (Player == null || Player.IsDead)
            return;

        var distance = GlobalPosition.DistanceTo(Player.GlobalPosition);

        switch (CurrentState)
        {
            case EnemyState.Patrol:
                if (DetectionRange > 0 && distance <= AggroDistance)
                    SetState(EnemyState.Alert);
                break;
            case EnemyState.Alert:
                if (distance > AggroDistance * 1.5f)
                    SetState(EnemyState.Patrol);
                else if (distance <= AggroDistance * 0.7f)
                    SetState(EnemyState.Chase);
                break;
            case EnemyState.Chase:
                if (distance > AggroDistance * 1.5f)
                    SetState(EnemyState.Patrol);
                else if (distance <= AttackRange)
                    SetState(EnemyState.Attack);
                break;
            case EnemyState.Attack:
                if (Player.IsDead || distance > AttackRange * 1.5f)
                    SetState(EnemyState.Patrol);
                break;
        }
    }

    private void FireBullet()
    {
        if (_bulletScene == null || Player == null) return;

        var bullet = _bulletScene.Instantiate<BulletProjectile>();
        GetParent().AddChild(bullet);

        var dir = (Player.GlobalPosition - GlobalPosition).Normalized();
        bullet.Initialize(GlobalPosition, dir, 240.0f, ContactDamage);

        // Visual feedback
        _firePulseTimer = 0.15f;
        var flash = new MuzzleFlash();
        AddChild(flash);
        flash.GlobalPosition = GlobalPosition + new Vector2(Scale.X * 12, 0);
    }

    protected override bool IsHoverMover() => true;
    protected override float GetSeparationRadius() => 16.0f;
    protected override float GetSeparationStrength() => 35.0f;
}
