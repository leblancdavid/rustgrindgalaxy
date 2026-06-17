using Godot;

public partial class BulletProjectile : Area2D
{
    [Export] public float Speed { get; set; } = 120.0f;
    [Export] public int Damage { get; set; } = 1;
    [Export] public float MaxLifetime { get; set; } = 3.0f;

    private Vector2 _direction;

    public void Initialize(Vector2 position, Vector2 direction, float? speed = null, int? damage = null)
    {
        GlobalPosition = position;
        _direction = direction.Normalized();
        if (speed.HasValue) Speed = speed.Value;
        if (damage.HasValue) Damage = damage.Value;
        Rotation = _direction.Angle();
    }

    public override void _Ready()
    {
        BodyEntered += OnHit;
        AreaEntered += OnHit;
    }

    public override void _Process(double delta)
    {
        Position += _direction * Speed * (float)delta;
        MaxLifetime -= (float)delta;
        if (MaxLifetime <= 0)
            QueueFree();
    }

    private void OnHit(Node2D body)
    {
        if (body is PlayerController player)
        {
            player.TakeDamage(Damage);
        }
        QueueFree();
    }
}
