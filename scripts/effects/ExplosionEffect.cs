using Godot;

public partial class ExplosionEffect : AnimatedSprite2D
{
    [Export] public int DamageRadius { get; set; } = 0;
    [Export] public int DamageAmount { get; set; } = 0;

    private Area2D? _hitbox;

    public override void _Ready()
    {
        BuildSpriteFrames();

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

        AnimationFinished += () => QueueFree();
        Play();
    }

    private void BuildSpriteFrames()
    {
        var dir = "res://animations/explosions/Explosion_1/";
        var frames = new SpriteFrames();
        frames.AddAnimation("default");
        frames.SetAnimationSpeed("default", 20.0);
        for (var i = 1; i <= 10; i++)
        {
            var tex = GD.Load<Texture2D>($"{dir}Explosion_{i}.png");
            if (tex != null)
                frames.AddFrame("default", tex, 0.05f);
        }
        if (frames.GetFrameCount("default") > 0)
            SpriteFrames = frames;
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
