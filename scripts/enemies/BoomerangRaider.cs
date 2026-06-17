using Godot;

public partial class BoomerangRaider : EnemyBase
{
    [Export] public float MoveSpeed { get; set; } = 38.0f;
    [Export] public float PatrolDistance { get; set; } = 36.0f;
    [Export] public float ThrowRange { get; set; } = 120.0f;
    [Export] public float ThrowCooldown { get; set; } = 2.5f;
    [Export] public float BoomerangSpeed { get; set; } = 90.0f;

    private float _spawnX;
    private float _direction = 1.0f;
    private float _cooldownTimer;
    private bool _isThrowing;
    private float _throwTimer;
    private PackedScene? _boomerangScene;

    public override void _Ready()
    {
        base._Ready();
        _spawnX = GlobalPosition.X;
        _boomerangScene = GD.Load<PackedScene>("res://scenes/projectiles/BoomerangProjectile.tscn");
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
                if (DetectionRange > 0 && distance <= DetectionRange)
                    SetState(EnemyState.Alert);
                break;
            case EnemyState.Alert:
                if (distance > DetectionRange * 1.5f)
                    SetState(EnemyState.Patrol);
                else if (distance <= DetectionRange * 0.7f)
                    SetState(EnemyState.Chase);
                break;
            case EnemyState.Chase:
                if (distance > DetectionRange * 1.5f)
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
    }
}
