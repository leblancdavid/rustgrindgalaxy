using Godot;

public partial class RailGuard : EnemyBase
{
    [Export] public float RailSpeed { get; set; } = 30.0f;
    [Export] public float AttackCooldown { get; set; } = 3.0f;
    [Export] public float BoltSpeed { get; set; } = 200.0f;
    [Export] public int BoltDamage { get; set; } = 1;
    [Export] public float TelegraphTime { get; set; } = 0.5f;
    [Export] public float VulnerableTime { get; set; } = 1.0f;

    private PathFollow2D? _pathFollow;
    private float _cooldownTimer;
    private bool _isTelegraphing;
    private float _telegraphTimer;
    private bool _isVulnerable;
    private float _vulnerableTimer;
    private Polygon2D? _glowVisual;
    private PackedScene? _bulletScene;

    public override void _Ready()
    {
        base._Ready();
        _pathFollow = GetNodeOrNull<PathFollow2D>("PathFollow2D");
        _glowVisual = GetNodeOrNull<Polygon2D>("GlowVisual");
        _bulletScene = GD.Load<PackedScene>("res://scenes/projectiles/BulletProjectile.tscn");
        _cooldownTimer = 1.0f;

        if (_glowVisual != null)
            _glowVisual.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (CurrentState == EnemyState.Dead)
            return;

        base._Process(delta);

        if (_isVulnerable)
        {
            _vulnerableTimer -= (float)delta;
            if (_vulnerableTimer <= 0)
            {
                _isVulnerable = false;
                if (_glowVisual != null)
                    _glowVisual.Visible = false;
            }
        }
    }

    protected override void UpdatePatrolState(float delta)
    {
        UpdateRailMovement(delta);
    }

    protected override void UpdateChaseState(float delta)
    {
        UpdateRailMovement(delta);

        FacePlayer();

        if (_isTelegraphing)
        {
            _telegraphTimer -= delta;
            if (_telegraphTimer <= 0)
            {
                _isTelegraphing = false;
                FireBolt();
                _isVulnerable = true;
                _vulnerableTimer = VulnerableTime;
                if (_glowVisual != null)
                {
                    _glowVisual.Visible = true;
                    _glowVisual.Color = Colors.White;
                }
                SetState(EnemyState.Attack);
            }
            return;
        }

        if (!_isVulnerable)
        {
            _cooldownTimer -= delta;
            if (_cooldownTimer <= 0)
            {
                _cooldownTimer = AttackCooldown;
                StartTelegraph();
            }
        }
    }

    protected override void UpdateAttackState(float delta)
    {
        UpdateRailMovement(delta);

        if (_isVulnerable)
        {
            _vulnerableTimer -= delta;
            if (_vulnerableTimer <= 0)
            {
                _isVulnerable = false;
                if (_glowVisual != null)
                    _glowVisual.Visible = false;
            }
            return;
        }

        _cooldownTimer -= delta;
        if (_cooldownTimer <= 0)
        {
            SetState(EnemyState.Chase);
        }
    }

    private void UpdateRailMovement(float delta)
    {
        if (_pathFollow == null) return;
        _pathFollow.Progress += RailSpeed * delta;
        var targetPos = _pathFollow.GlobalPosition;
        var toTarget = targetPos - GlobalPosition;
        var dist = toTarget.Length();
        if (dist < 0.01f)
        {
            var snapped = GlobalPosition;
            snapped.X = targetPos.X;
            GlobalPosition = snapped;
            return;
        }
        var step = Mathf.Min(dist, RailSpeed * delta);
        Velocity = (toTarget / dist) * (step / Mathf.Max(delta, 0.0001f));
        MoveAndSlide();
        var pos = GlobalPosition;
        pos.X = targetPos.X;
        GlobalPosition = pos;
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
                if (distance > AggroDistance * 2.0f)
                    SetState(EnemyState.Patrol);
                break;
        }
    }

    private void StartTelegraph()
    {
        _isTelegraphing = true;
        _telegraphTimer = TelegraphTime;
        if (_glowVisual != null)
        {
            _glowVisual.Visible = true;
            _glowVisual.Color = new Color(1, 0.3f, 0.1f, 1);
        }
    }

    private void FireBolt()
    {
        if (_bulletScene == null) return;

        var dir = new Vector2(1, 0).Rotated(Rotation);
        var bolt = _bulletScene.Instantiate<BulletProjectile>();
        GetParent().AddChild(bolt);
        bolt.Initialize(GlobalPosition + dir * 8, dir, BoltSpeed, BoltDamage);
    }
}
