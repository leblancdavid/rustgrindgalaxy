using Godot;

public partial class BulletProjectile : Area2D
{
    [Export] public float Speed { get; set; } = 120.0f;
    [Export] public int Damage { get; set; } = 1;
    [Export] public float MaxLifetime { get; set; } = 3.0f;

    private Vector2 _direction;
    private Polygon2D? _visual;
    private Polygon2D? _glow;
    private Polygon2D? _trail;
    private float _totalTime;

    public void Initialize(Vector2 position, Vector2 direction, float? speed = null, int? damage = null)
    {
        GlobalPosition = position;
        _direction = direction.Normalized();
        if (speed.HasValue) Speed = speed.Value;
        if (damage.HasValue) Damage = damage.Value;
    }

    public override void _Ready()
    {
        BodyEntered += OnHit;
        AreaEntered += OnHit;
        _visual = GetNodeOrNull<Polygon2D>("Visual");
        _glow = GetNodeOrNull<Polygon2D>("Glow");
        _trail = GetNodeOrNull<Polygon2D>("Trail");
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _totalTime += dt;

        Position += _direction * Speed * dt;

        if (_visual != null)
            _visual.Rotation += dt * 12.0f;

        if (_glow != null)
        {
            var pulse = 0.7f + Mathf.Sin(_totalTime * 15.0f) * 0.3f;
            _glow.Color = new Color(1, 0.8f, 0.2f, pulse * 0.25f);
            _glow.Scale = new Vector2(1, 1) * (1.0f + Mathf.Sin(_totalTime * 10.0f) * 0.15f);
        }

        if (_trail != null)
            _trail.Rotation = _direction.Angle();

        MaxLifetime -= dt;
        if (MaxLifetime <= 0)
        {
            SpawnHitSpark();
            QueueFree();
        }
    }

    private void OnHit(Node2D body)
    {
        if (body is PlayerController player)
            player.TakeDamage(Damage);
        SpawnHitSpark();
        QueueFree();
    }

    private void SpawnHitSpark()
    {
        var spark = new HitSpark();
        GetParent()?.AddChild(spark);
        if (spark != null)
            spark.GlobalPosition = GlobalPosition;
    }
}
