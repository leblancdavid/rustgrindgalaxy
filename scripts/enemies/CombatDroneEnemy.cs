using Godot;

public partial class CombatDroneEnemy : EnemyBase
{
    [Export] public float HoverSpeed { get; set; } = 20.0f;
    [Export] public float BulletSpeed { get; set; } = 120.0f;
    [Export] public float BurstCooldown { get; set; } = 1.5f;
    [Export] public int BurstCount { get; set; } = 3;
    [Export] public float BurstSpacing { get; set; } = 0.15f;
    [Export] public float CombatRange { get; set; } = 100.0f;
    [Export] public float RetreatRange { get; set; } = 40.0f;
    [Export] public float HoverAmplitude { get; set; } = 6.0f;

    private float _time;
    private float _burstTimer;
    private int _shotsFired;
    private bool _isFiring;
    private PackedScene? _bulletScene;

    public override void _Ready()
    {
        base._Ready();
        _bulletScene = GD.Load<PackedScene>("res://scenes/projectiles/BulletProjectile.tscn");
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
        pos.Y += Mathf.Sin(_time * HoverSpeed * 0.5f) * (HoverAmplitude * 0.5f);
        GlobalPosition = pos;
    }

    protected override void UpdateChaseState(float delta)
    {
        _time += delta;

        if (Player == null) return;

        var offsetX = Scale.X * CombatRange * -0.5f;
        var targetX = Player.GlobalPosition.X + offsetX;
        var targetY = Player.GlobalPosition.Y - 20.0f + Mathf.Sin(_time * HoverSpeed * 0.7f) * HoverAmplitude;

        var pos = GlobalPosition;
        pos.X = Mathf.Lerp(pos.X, targetX, delta * 2.0f);
        pos.Y = Mathf.Lerp(pos.Y, targetY, delta * 1.5f);
        GlobalPosition = pos;

        FaceDirection(Player.GlobalPosition.X - GlobalPosition.X);

        var distance = GlobalPosition.DistanceTo(Player.GlobalPosition);
        if (distance <= CombatRange)
        {
            SetState(EnemyState.Attack);
        }
    }

    protected override void UpdateAttackState(float delta)
    {
        _time += delta;

        if (Player == null)
        {
            SetState(EnemyState.Patrol);
            return;
        }

        // Hover in place while firing
        var pos = GlobalPosition;
        pos.Y += Mathf.Sin(_time * HoverSpeed) * HoverAmplitude * 0.3f;
        GlobalPosition = pos;

        FacePlayer();

        if (!_isFiring)
        {
            _burstTimer -= delta;
            if (_burstTimer <= 0)
            {
                _isFiring = true;
                _shotsFired = 0;
            }
        }
        else
        {
            _burstTimer -= delta;
            if (_burstTimer <= 0 && _shotsFired < BurstCount)
            {
                FireBullet();
                _shotsFired++;
                _burstTimer = BurstSpacing;
            }

            if (_shotsFired >= BurstCount)
            {
                _isFiring = false;
                _burstTimer = BurstCooldown;
                _shotsFired = 0;

                // Retreat after burst
                SetState(EnemyState.Chase);
            }
        }

        var distance = GlobalPosition.DistanceTo(Player.GlobalPosition);
        if (distance <= RetreatRange)
        {
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
                    SetState(EnemyState.Chase);
                break;
            case EnemyState.Chase:
                if (distance > DetectionRange * 1.5f)
                    SetState(EnemyState.Patrol);
                break;
            case EnemyState.Attack:
                if (distance > CombatRange * 1.5f)
                    SetState(EnemyState.Chase);
                break;
        }
    }

    private void FireBullet()
    {
        if (_bulletScene == null || Player == null) return;

        var randomOffset = new Vector2(
            (float)GD.RandRange(-0.1f, 0.1f),
            (float)GD.RandRange(-0.1f, 0.1f)
        );

        var bullet = _bulletScene.Instantiate<BulletProjectile>();
        GetParent().AddChild(bullet);
        var dir = (Player.GlobalPosition - GlobalPosition + randomOffset).Normalized();
        bullet.Initialize(GlobalPosition, dir, BulletSpeed, ContactDamage);
    }
}
