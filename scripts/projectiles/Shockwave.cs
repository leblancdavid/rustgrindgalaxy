using Godot;

public partial class Shockwave : Area2D
{
    [Export] public float ExpandSpeed { get; set; } = 60.0f;
    [Export] public float MaxRadius { get; set; } = 80.0f;
    [Export] public int Damage { get; set; } = 1;

    private CollisionShape2D? _shape;
    private float _radius;
    private bool _hasHit;

    public void Initialize(Vector2 position, float? speed = null, int? damage = null)
    {
        GlobalPosition = position;
        if (speed.HasValue) ExpandSpeed = speed.Value;
        if (damage.HasValue) Damage = damage.Value;
    }

    public override void _Ready()
    {
        _shape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _radius += ExpandSpeed * dt;

        if (_radius >= MaxRadius)
        {
            QueueFree();
            return;
        }

        if (_shape?.Shape is CircleShape2D circle)
        {
            circle.Radius = _radius;
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_hasHit) return;
        if (body is PlayerController player)
        {
            player.TakeDamage(Damage);
            _hasHit = true;
        }
    }
}
