using Godot;

public partial class MineLayer : EnemyBase
{
    [Export] public float MoveSpeed { get; set; } = 25.0f;
    [Export] public float PatrolDistance { get; set; } = 30.0f;
    [Export] public float MineDropInterval { get; set; } = 2.0f;
    [Export] public int MaxMinesAlive { get; set; } = 4;
    [Export] public float ShieldRechargeTime { get; set; } = 3.0f;

    private float _spawnX;
    private float _direction = 1.0f;
    private float _dropTimer;
    private int _minesPlaced;
    private bool _shieldActive = true;
    private float _shieldTimer;
    private PackedScene? _mineScene;
    private Polygon2D? _shieldVisual;

    public override void _Ready()
    {
        base._Ready();
        _spawnX = GlobalPosition.X;
        _mineScene = GD.Load<PackedScene>("res://scenes/projectiles/Mine.tscn");
        _shieldVisual = GetNodeOrNull<Polygon2D>("Sprite/ShieldVisual");
        UpdateShieldVisual();
    }

    public override void _Process(double delta)
    {
        if (CurrentState == EnemyState.Dead) return;
        base._Process(delta);

        if (!_shieldActive)
        {
            _shieldTimer -= (float)delta;
            if (_shieldTimer <= 0)
            {
                _shieldActive = true;
                UpdateShieldVisual();
            }
        }
    }

    public override void TakeDamage(int amount, Node2D? damageSource = null)
    {
        if (_shieldActive)
        {
            _shieldActive = false;
            _shieldTimer = ShieldRechargeTime;
            UpdateShieldVisual();
            HurtFlashTimer = 0.1f;
            return;
        }

        base.TakeDamage(amount, damageSource);
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
        ApplyRampAdhesion(ref velocity, delta);
        Velocity = velocity;
        MoveAndSlide();

        _dropTimer -= delta;
        if (_dropTimer <= 0)
        {
            _dropTimer = MineDropInterval;
            DropMine();
        }

        // Track mines in scene
        CountPlacedMines();
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
                break;
        }
    }

    private void DropMine()
    {
        if (_mineScene == null) return;
        if (_minesPlaced >= MaxMinesAlive) return;

        var mine = _mineScene.Instantiate<Mine>();
        GetParent().AddChild(mine);
        mine.GlobalPosition = GlobalPosition + new Vector2(0, 4);
        _minesPlaced = CountPlacedMines();
    }

    private int CountPlacedMines()
    {
        var count = 0;
        foreach (var child in GetParent().GetChildren())
        {
            if (child is Mine mine && !mine.IsQueuedForDeletion())
                count++;
        }
        _minesPlaced = count;
        return count;
    }

    private void UpdateShieldVisual()
    {
        if (_shieldVisual == null) return;
        _shieldVisual.Visible = _shieldActive;
    }
}
