using Godot;

public partial class BoomerangRaider : EnemyBase
{
    [Export] public float MoveSpeed { get; set; } = 38.0f;
    [Export] public float PatrolDistance { get; set; } = 36.0f;
    [Export] public float ThrowRange { get; set; } = 320.0f;
    [Export] public float ThrowCooldown { get; set; } = 2.5f;
    [Export] public float BoomerangSpeed { get; set; } = 90.0f;

    private float _spawnX;
    private float _direction = 1.0f;
    private float _cooldownTimer;
    private bool _isThrowing;
    private float _throwTimer;
    private PackedScene? _boomerangScene;
    private Polygon2D? _visual;
    private Color _visualBaseColor;
    private float _throwAnimTimer;

    public override void _Ready()
    {
        base._Ready();
        _spawnX = GlobalPosition.X;
        _boomerangScene = GD.Load<PackedScene>("res://scenes/projectiles/BoomerangProjectile.tscn");
        _visual = GetNodeOrNull<Polygon2D>("Sprite/Visual");
        if (_visual != null)
            _visualBaseColor = _visual.Color;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_throwAnimTimer > 0 && _visual != null)
        {
            _throwAnimTimer -= (float)delta;
            var t = 1.0f - (_throwAnimTimer / 0.3f);
            var p = 1.0f - t;

            var tiltAngle = Mathf.Sin(t * Mathf.Pi) * 0.4f;
            _visual.Rotation = tiltAngle * (FacingNode?.Scale.X ?? Scale.X);

            _visual.Color = _visualBaseColor.Lerp(new Color(1, 1, 0.6f), p);

            var sq = 1.0f - Mathf.Sin(t * Mathf.Pi) * 0.2f;
            _visual.Scale = new Vector2(1.0f / sq, sq);
        }
    }

    protected override void UpdatePatrolState(float delta)
    {
        FaceDirection(_direction);

        var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity") * GravityScale;
        var velocity = Velocity;
        if (!IsOnFloor())
            velocity.Y += gravity * delta;

        var minX = _spawnX - PatrolDistance;
        var maxX = _spawnX + PatrolDistance;
        if (GlobalPosition.X <= minX)
            _direction = 1.0f;
        else if (GlobalPosition.X >= maxX)
            _direction = -1.0f;

        velocity.X = _direction * MoveSpeed;
        _desiredHorizontalVelocity = new Vector2(velocity.X, velocity.Y);
        ApplyRampAdhesion(ref velocity, delta);
        Velocity = velocity;
        MoveAndSlide();
    }

    protected override void UpdateChaseState(float delta)
    {
        FacePlayer();

        var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity") * GravityScale;
        var velocity = Velocity;
        if (!IsOnFloor())
            velocity.Y += gravity * delta;

        if (Player != null)
        {
            var dir = Mathf.Sign(Player.GlobalPosition.X - GlobalPosition.X);
            velocity.X = dir * MoveSpeed;
        }

        _desiredHorizontalVelocity = new Vector2(velocity.X, velocity.Y);
        ApplyRampAdhesion(ref velocity, delta);
        Velocity = velocity;
        MoveAndSlide();

        _cooldownTimer -= delta;
        if (_cooldownTimer <= 0 && Player != null &&
            GlobalPosition.DistanceTo(Player.GlobalPosition) <= ThrowRange)
        {
            SetState(EnemyState.Attack);
        }
    }

    protected override void UpdateAttackState(float delta)
    {
        FacePlayer();

        var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity") * GravityScale;
        var velocity = Velocity;
        if (!IsOnFloor())
            velocity.Y += gravity * delta;
        velocity.X = 0;
        ApplyRampAdhesion(ref velocity, delta);
        Velocity = velocity;
        MoveAndSlide();

        if (!_isThrowing)
        {
            _isThrowing = true;
            _throwTimer = 0.3f;
        }

        _throwTimer -= delta;
        if (_throwTimer <= 0)
        {
            ThrowBoomerang();
            _isThrowing = false;
            _cooldownTimer = ThrowCooldown;
            SetState(EnemyState.Chase);
        }
    }

    protected override void CheckTransitions()
    {
        if (Player == null || Player.IsDead) return;

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
                break;
            case EnemyState.Attack:
                if (distance > ThrowRange * 1.5f)
                {
                    _isThrowing = false;
                    SetState(EnemyState.Chase);
                }
                break;
        }
    }

    private void ThrowBoomerang()
    {
        if (_boomerangScene == null || Player == null) return;

        var boomerang = _boomerangScene.Instantiate<BoomerangProjectile>();
        GetParent().AddChild(boomerang);

        var dir = new Vector2(Scale.X, 0);
        boomerang.Initialize(GlobalPosition, dir, BoomerangSpeed, ContactDamage);

        _throwAnimTimer = 0.3f;
    }
}
