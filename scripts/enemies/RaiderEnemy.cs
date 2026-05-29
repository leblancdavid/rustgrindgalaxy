using Godot;

public partial class RaiderEnemy : CharacterBody2D
{
    [Export] public float MoveSpeed = 42.0f;
    [Export] public float PatrolDistance = 36.0f;
    [Export] public float GravityScale = 1.0f;
    [Export] public int ContactDamage = 1;

    private float _spawnX;
    private float _direction = 1.0f;
    private Area2D _hurtArea = null!;

    public override void _Ready()
    {
        _spawnX = GlobalPosition.X;
        _hurtArea = GetNode<Area2D>("HurtArea");
        _hurtArea.BodyEntered += OnHurtAreaBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        var velocity = Velocity;
        var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity") * GravityScale;

        if (!IsOnFloor())
        {
            velocity.Y += gravity * (float)delta;
        }

        var minX = _spawnX - PatrolDistance;
        var maxX = _spawnX + PatrolDistance;
        if (GlobalPosition.X <= minX)
        {
            _direction = 1.0f;
        }
        else if (GlobalPosition.X >= maxX)
        {
            _direction = -1.0f;
        }

        velocity.X = _direction * MoveSpeed;
        Velocity = velocity;
        MoveAndSlide();
    }

    private void OnHurtAreaBodyEntered(Node2D body)
    {
        if (body is PlayerController player)
        {
            player.TakeDamage(ContactDamage);
        }
    }
}
