using Godot;

public partial class RaiderEnemy : EnemyBase
{
    [Export] public float MoveSpeed { get; set; } = 42.0f;
    [Export] public float PatrolDistance { get; set; } = 36.0f;
    [Export] public float AttackRange { get; set; } = 24.0f;
    [Export] public float AttackCooldown { get; set; } = 1.5f;
    [Export] public float ChaseSpeed { get; set; } = 52.0f;

    private float _spawnX;
    private float _direction = 1.0f;
    private float _attackCooldownTimer;
    private Area2D? _meleeHitbox;
    private Polygon2D? _bodyVisual;
    private Polygon2D? _headVisual;
    private float _attackAnimTimer;

    public override void _Ready()
    {
        base._Ready();
        _spawnX = GlobalPosition.X;
        _meleeHitbox = GetNodeOrNull<Area2D>("MeleeHitbox");
        if (_meleeHitbox != null)
        {
            _meleeHitbox.Monitoring = false;
            _meleeHitbox.BodyEntered += OnMeleeHit;
        }
        _bodyVisual = GetNodeOrNull<Polygon2D>("Sprite/Visual");
        _headVisual = GetNodeOrNull<Polygon2D>("Sprite/Head");
    }

    protected override void UpdatePatrolState(float delta)
    {
        FaceDirection(_direction);

        var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity") * GravityScale;

        var velocity = Velocity;
        if (!IsOnFloor())
        {
            velocity.Y += gravity * delta;
        }

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

    protected override void UpdateAlertState(float delta)
    {
        FacePlayer();
        Velocity = new Vector2(0, Velocity.Y);
        var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity") * GravityScale;
        if (!IsOnFloor())
        {
            var velocity = Velocity;
            velocity.Y += gravity * delta;
            Velocity = velocity;
        }
        MoveAndSlide();
    }

    protected override void UpdateChaseState(float delta)
    {
        FacePlayer();

        var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity") * GravityScale;
        var velocity = Velocity;
        if (!IsOnFloor())
        {
            velocity.Y += gravity * delta;
        }

        if (Player != null)
        {
            var dir = Mathf.Sign(Player.GlobalPosition.X - GlobalPosition.X);
            velocity.X = dir * ChaseSpeed;
        }

        _desiredHorizontalVelocity = new Vector2(velocity.X, velocity.Y);
        ApplyRampAdhesion(ref velocity, delta);
        Velocity = velocity;
        MoveAndSlide();

        if (Player != null && GlobalPosition.DistanceTo(Player.GlobalPosition) <= AttackRange)
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
        {
            velocity.Y += gravity * delta;
        }
        velocity.X = 0;
        ApplyRampAdhesion(ref velocity, delta);
        Velocity = velocity;
        MoveAndSlide();

        // Body follow-through animation
        if (_attackAnimTimer > 0)
        {
            _attackAnimTimer -= delta;
            var t = 1.0f - (_attackAnimTimer / 0.25f);
            var sq = 1.0f - Mathf.Sin(t * Mathf.Pi) * 0.25f;
            var v = new Vector2(1.0f / sq, sq);
            if (_bodyVisual != null)
                _bodyVisual.Scale = v;
            if (_headVisual != null)
                _headVisual.Scale = new Vector2(1.0f / sq, sq);
        }

        _attackCooldownTimer -= delta;

        if (_attackCooldownTimer <= 0)
        {
            _attackCooldownTimer = AttackCooldown;
            _attackAnimTimer = 0.25f;
            PerformMeleeAttack();
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
                if (distance > AttackRange * 1.3f)
                    SetState(EnemyState.Chase);
                break;
        }
    }

    private void PerformMeleeAttack()
    {
        if (_meleeHitbox == null) return;

        _meleeHitbox.Monitoring = true;

        var slashScene = GD.Load<PackedScene>("res://scenes/effects/SlashEffect.tscn");
        if (slashScene != null)
        {
            var slash = slashScene.Instantiate<SlashEffect>();
            AddChild(slash);
            slash.GlobalPosition = GlobalPosition + new Vector2(0, -12);
        }

        var timer = GetTree().CreateTimer(0.2f);
        timer.Timeout += () =>
        {
            if (_meleeHitbox != null)
                _meleeHitbox.Monitoring = false;
        };
    }

    private void OnMeleeHit(Node2D body)
    {
        if (body is PlayerController player)
        {
            player.TakeDamage(ContactDamage);
        }
    }
}
