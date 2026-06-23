using Godot;

public partial class ShockDrone : EnemyBase
{
    [Export] public float TelegraphTime { get; set; } = 1.0f;
    [Export] public float AttackCooldown { get; set; } = 4.0f;
    [Export] public float VulnerableTime { get; set; } = 1.5f;
    [Export] public float ShockwaveSpeed { get; set; } = 60.0f;
    [Export] public float HoverAmplitude { get; set; } = 6.0f;

    private float _time;
    private float _telegraphTimer;
    private float _cooldownTimer;
    private bool _isCharging;
    private bool _isVulnerable;
    private float _vulnerableTimer;
    private float _spawnY;
    private PackedScene? _shockwaveScene;
    private Polygon2D? _chargeVisual;

    public override void _Ready()
    {
        base._Ready();
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        InitializeHoverVariance(rng, HoverAmplitude * 0.5f);
        _spawnY = GlobalPosition.Y;
        _shockwaveScene = GD.Load<PackedScene>("res://scenes/projectiles/Shockwave.tscn");
        _chargeVisual = GetNodeOrNull<Polygon2D>("ChargeVisual");
        if (_chargeVisual != null)
            _chargeVisual.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (CurrentState == EnemyState.Dead) return;
        base._Process(delta);

        if (_isVulnerable)
        {
            _vulnerableTimer -= (float)delta;
            if (_vulnerableTimer <= 0)
            {
                _isVulnerable = false;
                _cooldownTimer = AttackCooldown;
                SetState(EnemyState.Patrol);
            }
        }
    }

    protected override void UpdatePatrolState(float delta)
    {
        _time += delta;

        Velocity = new Vector2(0f, 1.5f * Mathf.Cos(_time * 1.5f + _hoverPhase) * (HoverAmplitude * _hoverAmplitudeScale));
        MoveAndSlide();
        ClampAboveFloor(30f);

        _cooldownTimer -= delta;
        if (_cooldownTimer <= 0 && Player != null &&
            GlobalPosition.DistanceTo(Player.GlobalPosition) <= AggroDistance)
        {
            SetState(EnemyState.Alert);
        }
    }

    protected override void UpdateAlertState(float delta)
    {
        _time += delta;

        Velocity = new Vector2(0f, 1.5f * Mathf.Cos(_time * 1.5f + _hoverPhase) * (HoverAmplitude * _hoverAmplitudeScale));
        MoveAndSlide();
        ClampAboveFloor(30f);

        FacePlayer();

        if (!_isCharging)
        {
            _isCharging = true;
            _telegraphTimer = TelegraphTime;
            if (_chargeVisual != null)
            {
                _chargeVisual.Visible = true;
                _chargeVisual.Color = new Color(1, 1, 0.3f, 0.3f);
            }
        }

        _telegraphTimer -= delta;
        var chargeProgress = 1.0f - (_telegraphTimer / TelegraphTime);
        if (_chargeVisual != null)
        {
            _chargeVisual.Color = new Color(1, 1 - chargeProgress * 0.7f, 0.3f, 0.3f + chargeProgress * 0.7f);
        }

        if (_telegraphTimer <= 0)
        {
            FireShockwave();
            _isCharging = false;
            _isVulnerable = true;
            _vulnerableTimer = VulnerableTime;
            if (_chargeVisual != null)
                _chargeVisual.Visible = false;
            SetState(EnemyState.Attack);
        }
    }

    protected override void UpdateAttackState(float delta)
    {
        _time += delta;

        Velocity = new Vector2(0f, 1.5f * Mathf.Cos(_time * 1.5f + _hoverPhase) * (HoverAmplitude * _hoverAmplitudeScale));
        MoveAndSlide();
        ClampAboveFloor(30f);
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
                if (Player.IsDead || distance > AggroDistance * 1.5f)
                {
                    _isCharging = false;
                    if (_chargeVisual != null)
                        _chargeVisual.Visible = false;
                    SetState(EnemyState.Patrol);
                }
                break;
            case EnemyState.Attack:
                if (_isVulnerable && _vulnerableTimer <= 0)
                {
                    _isVulnerable = false;
                    _cooldownTimer = AttackCooldown;
                    SetState(EnemyState.Patrol);
                }
                break;
        }
    }

    private void FireShockwave()
    {
        if (_shockwaveScene == null) return;

        var groundY = GetGroundY();
        var shockwave = _shockwaveScene.Instantiate<Shockwave>();
        GetParent().AddChild(shockwave);
        shockwave.Initialize(
            new Vector2(GlobalPosition.X, groundY),
            ShockwaveSpeed,
            ContactDamage
        );
    }

    private float GetGroundY()
    {
        var spaceState = GetWorld2D().DirectSpaceState;
        var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + new Vector2(0, 200), 1);
        var result = spaceState.IntersectRay(query);
        return result.ContainsKey("position")
            ? ((Vector2)result["position"]).Y
            : GlobalPosition.Y + 100;
    }

    protected override bool IsHoverMover() => true;
    protected override float GetSeparationRadius() => 16.0f;
    protected override float GetSeparationStrength() => 35.0f;
}
