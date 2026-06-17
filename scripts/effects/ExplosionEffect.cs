using Godot;

public partial class ExplosionEffect : AnimatedSprite2D
{
    [Export] public int DamageRadius { get; set; } = 0;
    [Export] public int DamageAmount { get; set; } = 0;

    private Area2D? _hitbox;

    public override void _Ready()
    {
        if (DamageRadius > 0)
        {
            _hitbox = new Area2D();
            _hitbox.CollisionMask = 2;
            var shape = new CollisionShape2D();
            shape.Shape = new CircleShape2D { Radius = DamageRadius };
            _hitbox.AddChild(shape);
            AddChild(_hitbox);
            _hitbox.BodyEntered += OnBodyEntered;
        }

        Connect(AnimationPlayer.SignalName.AnimationFinished, Callable.From(() => QueueFree()));
        Play();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (DamageAmount <= 0)
            return;

        if (body is PlayerController player)
        {
            player.TakeDamage(DamageAmount);
        }
    }
}
