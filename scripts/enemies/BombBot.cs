using Godot;

public partial class BombBot : EnemyBase
{
    [Export] public float RollSpeed { get; set; } = 55.0f;
    [Export] public float SelfDestructTimer { get; set; } = 4.0f;
    [Export] public float ExplosionRadius { get; set; } = 48.0f;
    [Export] public int ExplosionDamage { get; set; } = 2;
    [Export] public float BeepAcceleration { get; set; } = 1.5f;

    private float _activationTimer;
    private bool _activated;
    private float _beepTimer;
    private Polygon2D? _glowVisual;

    public override void _Ready()
    {
        base._Ready();
        _glowVisual = GetNodeOrNull<Polygon2D>("GlowVisual");
        if (_glowVisual != null)
            _glowVisual.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (CurrentState == EnemyState.Dead)
            return;

        base._Process(delta);

        if (_activated)
            UpdateBeepVisual((float)delta);
    }

    protected override void UpdatePatrolState(float delta)
    {
        var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity") * GravityScale;
        var velocity = Velocity;
        if (!IsOnFloor())
            velocity.Y += gravity * delta;
        velocity.X = 0;
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
            velocity.X = dir * RollSpeed;
        }

        Velocity = velocity;
        MoveAndSlide();

        if (!_activated)
        {
            _activated = true;
            _activationTimer = SelfDestructTimer;
            if (_glowVisual != null)
                _glowVisual.Visible = true;
        }

        _activationTimer -= delta;
        if (_activationTimer <= 0)
        {
            Explode();
        }
    }

    protected override void CheckTransitions()
    {
        if (Player == null || Player.IsDead)
        {
            if (_activated)
            {
                _activated = false;
                if (_glowVisual != null)
                    _glowVisual.Visible = false;
            }
            return;
        }

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
                else
                    SetState(EnemyState.Chase);
                break;
            case EnemyState.Chase:
                if (Player.IsDead)
                    SetState(EnemyState.Patrol);
                break;
        }
    }

    protected override void Die()
    {
        Explode();
    }

    private void UpdateBeepVisual(float delta)
    {
        _beepTimer += delta;
        var remainingRatio = _activationTimer / SelfDestructTimer;
        var flashSpeed = 2.0f + (1.0f - remainingRatio) * BeepAcceleration;
        var intensity = 0.3f + (Mathf.Sin(_beepTimer * flashSpeed * Mathf.Pi * 2) * 0.5f + 0.5f) * 0.7f;

        if (_glowVisual != null)
            _glowVisual.Color = new Color(1, 0.3f * intensity, 0.1f, intensity);
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
