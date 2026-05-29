using Godot;

public partial class DroneEnemy : Area2D
{
    [Export] public float HoverAmplitude = 8.0f;
    [Export] public float HoverSpeed = 2.4f;
    [Export] public float PatrolDistance = 30.0f;
    [Export] public float PatrolSpeed = 24.0f;
    [Export] public int ContactDamage = 1;

    private float _spawnX;
    private float _spawnY;
    private float _direction = 1.0f;
    private float _time;

    public override void _Ready()
    {
        _spawnX = GlobalPosition.X;
        _spawnY = GlobalPosition.Y;
        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;

        var minX = _spawnX - PatrolDistance;
        var maxX = _spawnX + PatrolDistance;
        var position = GlobalPosition;

        if (position.X <= minX)
        {
            _direction = 1.0f;
        }
        else if (position.X >= maxX)
        {
            _direction = -1.0f;
        }

        position.X += _direction * PatrolSpeed * (float)delta;
        position.Y = _spawnY + (Mathf.Sin(_time * HoverSpeed) * HoverAmplitude);
        GlobalPosition = position;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is PlayerController player)
        {
            player.TakeDamage(ContactDamage);
        }
    }
}
