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
    private Polygon2D? _barrel;
    private Polygon2D? _visual;
    private Color _barrelBaseColor;
    private float _firePulseTimer;

    public override void _Ready()
    {
        base._Ready();
        _bulletScene = GD.Load<PackedScene>("res://scenes/projectiles/BulletProjectile.tscn");
        _barrel = GetNodeOrNull<Polygon2D>("Barrel");
        _visual = GetNodeOrNull<Polygon2D>("Visual");
        if (_barrel != null)
            _barrelBaseColor = _barrel.Color;
    }

    public override void _Process(double delta)
    {
        if (CurrentState == EnemyState.Dead) return;
        base._Process(delta);

        if (_firePulseTimer > 0)
        {
            _firePulseTimer -= (float)delta;
            if (_barrel != null)
            {
                var t = 1.0f - (_firePulseTimer / 0.12f);
                var p = 1.0f - t;
                _barrel.Color = _barrelBaseColor.Lerp(new Color(1, 1, 0.6f), p);
                _barrel.Scale = new Vector2(1, 1) * (1.0f + p * 0.2f);
            }
        }
    }

    protected override void UpdatePatrolState(float delta)
    {
        _time += delta;
        var hoverOmega = HoverSpeed * 0.5f;
        Velocity = new Vector2(0f, hoverOmega * Mathf.Cos(_time * hoverOmega) * (HoverAmplitude * 0.5f));
        MoveAndSlide();
        ClampAboveFloor(30f);
    }

    protected override void UpdateChaseState(float delta)
    {
        _time += delta;

        if (Player == null) return;

        var offsetX = Scale.X * CombatRange * -0.5f;
        var targetX = Player.GlobalPosition.X + offsetX;
        var targetY = Player.GlobalPosition.Y - 20.0f + Mathf.Sin(_time * HoverSpeed * 0.7f) * HoverAmplitude;

        Velocity = new Vector2(
            (targetX - GlobalPosition.X) * 2.0f,
            (targetY - GlobalPosition.Y) * 1.5f
        );
        MoveAndSlide();
        ClampAboveFloor(30f);

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

        Velocity = new Vector2(0f, HoverSpeed * Mathf.Cos(_time * HoverSpeed) * (HoverAmplitude * 0.3f));
        MoveAndSlide();
        ClampAboveFloor(30f);

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
                if (DetectionRange > 0 && distance <= AggroDistance)
                    SetState(EnemyState.Chase);
                break;
            case EnemyState.Chase:
                if (distance > AggroDistance * 1.5f)
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

        _firePulseTimer = 0.12f;
        var flash = new MuzzleFlash();
        AddChild(flash);
        flash.GlobalPosition = GlobalPosition + new Vector2(Scale.X * 12, -2);
    }
}
