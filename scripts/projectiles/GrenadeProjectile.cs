using Godot;

public partial class GrenadeProjectile : RigidBody2D
{
    [Export] public float FuseTime { get; set; } = 1.5f;
    [Export] public int Damage { get; set; } = 1;
    [Export] public float ExplosionRadius { get; set; } = 36.0f;

    private float _timer;
    private bool _hasBounced;

    public void Initialize(Vector2 position, Vector2 velocity, float? fuseTime = null, int? damage = null)
    {
        GlobalPosition = position;
        LinearVelocity = velocity;
        if (fuseTime.HasValue) FuseTime = fuseTime.Value;
        if (damage.HasValue) Damage = damage.Value;
    }

    public override void _Ready()
    {
        BodyEntered += OnRigidBodyEntered;
        _timer = FuseTime;
    }

    public override void _Process(double delta)
    {
        _timer -= (float)delta;
        if (_timer <= 0)
            Explode();
    }

    private void OnRigidBodyEntered(Node body)
    {
        if (!_hasBounced && body is StaticBody2D)
        {
            _hasBounced = true;
        }
    }

    private void Explode()
    {
        var explosionScene = GD.Load<PackedScene>("res://scenes/effects/ExplosionEffect.tscn");
        if (explosionScene != null)
        {
            var instance = explosionScene.Instantiate<ExplosionEffect>();
            GetParent().AddChild(instance);
            instance.GlobalPosition = GlobalPosition;
            instance.DamageRadius = (int)ExplosionRadius;
            instance.DamageAmount = Damage;
        }
        QueueFree();
    }
}
