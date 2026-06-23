using Godot;

public partial class SuicideDrone : EnemyBase
{
    [Export] public float DiveSpeed { get; set; } = 140.0f;
    [Export] public float ExplosionRadius { get; set; } = 32.0f;
    [Export] public int ExplosionDamage { get; set; } = 2;
    [Export] public float HoverAmplitude { get; set; } = 5.0f;
    [Export] public float MaxDiveTime { get; set; } = 1.5f;

    private float _time;
    private float _spawnY;
    private bool _lockedOn;
    private float _lockOnTimer;
    private Vector2 _diveTarget;
    private float _diveTimer;
    private Polygon2D? _lockVisual;
    private bool _hasDied;

    public override void _Ready()
    {
        base._Ready();
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        InitializeHoverVariance(rng, HoverAmplitude * 0.5f);
        _spawnY = GlobalPosition.Y;
        _lockVisual = GetNodeOrNull<Polygon2D>("LockVisual");
        if (_lockVisual != null)
            _lockVisual.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (CurrentState == EnemyState.Dead) return;
        base._Process(delta);
    }

    protected override void UpdatePatrolState(float delta)
    {
        _time += delta;
        Velocity = new Vector2(0f, 2.0f * Mathf.Cos(_time * 2.0f + _hoverPhase) * (HoverAmplitude * _hoverAmplitudeScale));
        MoveAndSlide();
        ClampAboveFloor(30f);
    }

    protected override void UpdateAlertState(float delta)
    {
        _time += delta;

        if (Player == null)
        {
            _lockedOn = false;
            SetState(EnemyState.Patrol);
            return;
        }

        Velocity = new Vector2(0f, 2.0f * Mathf.Cos(_time * 2.0f + _hoverPhase) * (HoverAmplitude * _hoverAmplitudeScale));
        MoveAndSlide();
        ClampAboveFloor(30f);

        FacePlayer();

        if (!_lockedOn)
        {
            _lockedOn = true;
            _lockOnTimer = 0.5f;
            if (_lockVisual != null)
                _lockVisual.Visible = true;
        }

        _lockOnTimer -= delta;

        if (_lockVisual != null)
        {
            var intensity = 0.3f + (Mathf.Sin(_time * 12.0f) * 0.5f + 0.5f) * 0.7f;
            _lockVisual.Color = new Color(1, 0.2f, 0.1f, intensity);
        }

        if (_lockOnTimer <= 0)
        {
            _diveTarget = Player.GlobalPosition;
            _diveTimer = MaxDiveTime;
            SetState(EnemyState.Chase);
        }
    }

    protected override void UpdateChaseState(float delta)
    {
        if (_lockVisual != null)
            _lockVisual.Visible = false;

        // Dive toward target
        var pos = GlobalPosition;
        var toTarget = _diveTarget - pos;
        var dir = toTarget.LengthSquared() > 0.01f ? toTarget.Normalized() : Vector2.Zero;
        _desiredHorizontalVelocity = dir * DiveSpeed;
        Velocity = dir * DiveSpeed;
        MoveAndSlide();
        FaceDirection(dir.X);

        _diveTimer -= delta;

        // Check for contact with player or terrain
        if (GlobalPosition.DistanceTo(_diveTarget) < 8.0f || GlobalPosition.Y > _spawnY + 150 || _diveTimer <= 0f)
        {
            Explode();
            return;
        }

        // Check if we hit the player
        if (Player != null && GlobalPosition.DistanceTo(Player.GlobalPosition) < 16.0f)
        {
            Player.TakeDamage(ExplosionDamage);
            Explode();
        }
    }

    protected override void CheckTransitions()
    {
        if (Player == null || Player.IsDead) return;

        var distance = GlobalPosition.DistanceTo(Player.GlobalPosition);

        switch (CurrentState)
        {
            case EnemyState.Patrol:
                if (distance <= AggroDistance)
                    SetState(EnemyState.Alert);
                break;
            case EnemyState.Alert:
                if (Player.IsDead || distance > AggroDistance * 1.5f)
                {
                    _lockedOn = false;
                    if (_lockVisual != null)
                        _lockVisual.Visible = false;
                    SetState(EnemyState.Patrol);
                }
                break;
            case EnemyState.Chase:
                if (Player.IsDead)
                    SetState(EnemyState.Patrol);
                break;
        }
    }

    protected override void Die()
    {
        if (_hasDied) return;
        _hasDied = true;
        Explode();
    }

    private void Explode()
    {
        var explosion = GD.Load<PackedScene>("res://scenes/effects/ExplosionEffect.tscn");
        if (explosion != null)
        {
            var instance = explosion.Instantiate<ExplosionEffect>();
            GetParent().AddChild(instance);
            instance.GlobalPosition = GlobalPosition;
            instance.DamageRadius = (int)ExplosionRadius;
            instance.DamageAmount = ExplosionDamage;
        }
        QueueFree();
    }

    protected override bool IsHoverMover() => true;
    protected override float GetSeparationRadius() => 18.0f;
    protected override float GetSeparationStrength() => 40.0f;
    protected override bool OnStuckAction() => false;
}
