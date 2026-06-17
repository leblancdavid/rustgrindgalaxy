using Godot;

public partial class Mine : Area2D
{
    [Export] public float ArmingTime { get; set; } = 0.5f;
    [Export] public int Damage { get; set; } = 2;
    [Export] public float ExplosionRadius { get; set; } = 24.0f;

    private bool _armed;
    private AnimatedSprite2D? _visual;

    public override void _Ready()
    {
        _visual = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        BodyEntered += OnBodyEntered;
        var timer = GetTree().CreateTimer(ArmingTime);
        timer.Timeout += () => _armed = true;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!_armed) return;

        if (body is PlayerController player)
        {
            Explode();
            player.TakeDamage(Damage);
        }
    }

    public void Explode()
    {
        var explosion = GD.Load<PackedScene>("res://scenes/effects/ExplosionEffect.tscn");
        if (explosion != null)
        {
            var instance = explosion.Instantiate<ExplosionEffect>();
            GetParent().AddChild(instance);
            instance.GlobalPosition = GlobalPosition;
            instance.DamageRadius = (int)ExplosionRadius;
            instance.DamageAmount = Damage;
        }
        QueueFree();
    }
}
