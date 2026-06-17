using Godot;

public partial class SuicideDrone : EnemyBase
{
    [Export] public float DiveSpeed { get; set; } = 140.0f;
    [Export] public float DetectionRangeOverride { get; set; } = 180.0f;
    [Export] public float ExplosionRadius { get; set; } = 32.0f;
    [Export] public int ExplosionDamage { get; set; } = 2;
    [Export] public float HoverAmplitude { get; set; } = 5.0f;

    private float _time;
    private float _spawnY;
    private bool _lockedOn;
    private float _lockOnTimer;
    private Vector2 _diveTarget;
    private Polygon2D? _lockVisual;
    private bool _hasDied;

    public override void _Ready()
    {
        base._Ready();
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
        var pos = GlobalPosition;
        pos.Y = _spawnY + Mathf.Sin(_time * 2.0f) * HoverAmplitude;
        GlobalPosition = pos;
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

        var pos = GlobalPosition;
        pos.Y = _spawnY + Mathf.Sin(_time * 2.0f) * HoverAmplitude;
        GlobalPosition = pos;

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
            SetState(EnemyState.Chase);
        }
    }

    protected override void UpdateChaseState(float delta)
    {
        if (_lockVisual != null)
            _lockVisual.Visible = false;

        // Dive toward target
        var pos = GlobalPosition;
        var dir = (_diveTarget - pos).Normalized();
        pos += dir * DiveSpeed * delta;

        // Check for contact with player or terrain
        if (pos.DistanceTo(_diveTarget) < 8.0f || pos.Y > _spawnY + 150)
        {
            Explode();
            return;
        }

        GlobalPosition = pos;
        FaceDirection(dir.X);

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
                if (distance <= DetectionRangeOverride)
                    SetState(EnemyState.Alert);
                break;
            case EnemyState.Alert:
                if (Player.IsDead || distance > DetectionRangeOverride * 1.5f)
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
}
