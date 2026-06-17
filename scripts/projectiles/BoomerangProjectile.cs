using Godot;

public partial class BoomerangProjectile : Area2D
{
    [Export] public float Speed { get; set; } = 90.0f;
    [Export] public int Damage { get; set; } = 1;
    [Export] public float MaxRange { get; set; } = 120.0f;

    private Vector2 _startPosition;
    private Vector2 _direction;
    private float _distanceTraveled;
    private bool _returning;

    public void Initialize(Vector2 position, Vector2 direction, float? speed = null, int? damage = null)
    {
        GlobalPosition = position;
        _startPosition = position;
        _direction = direction.Normalized();
        if (speed.HasValue) Speed = speed.Value;
        if (damage.HasValue) Damage = damage.Value;
        Rotation = _direction.Angle();
    }

    public override void _Ready()
    {
        BodyEntered += OnHit;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        var moveDir = _returning ? -_direction : _direction;
        Position += moveDir * Speed * dt;
        Rotation += dt * 8.0f;

        if (!_returning)
        {
            _distanceTraveled += Speed * dt;
            if (_distanceTraveled >= MaxRange)
                _returning = true;
        }
        else
        {
            var dist = GlobalPosition.DistanceTo(_startPosition);
            if (dist < 8.0f)
                QueueFree();
        }
    }

    private void OnHit(Node2D body)
    {
        if (body is PlayerController player)
        {
            player.TakeDamage(Damage);
            QueueFree();
        }
    }
}
