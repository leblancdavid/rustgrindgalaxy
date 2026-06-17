using Godot;

public partial class LaserBeam : Node2D
{
    [Export] public float BeamDuration { get; set; } = 0.3f;
    [Export] public float MaxLength { get; set; } = 600.0f;
    [Export] public int DamagePerTick { get; set; } = 1;
    [Export] public float TickInterval { get; set; } = 0.1f;

    private Line2D? _line;
    private Area2D? _hitArea;
    private float _timer;
    private float _tickTimer;
    private Vector2 _beamEnd;

    public void Initialize(Vector2 origin, Vector2 direction, float? duration = null)
    {
        GlobalPosition = origin;
        Rotation = direction.Angle();
        if (duration.HasValue) BeamDuration = duration.Value;
    }

    public override void _Ready()
    {
        _line = GetNodeOrNull<Line2D>("Line2D");
        _hitArea = GetNodeOrNull<Area2D>("HitArea");

        var spaceState = GetWorld2D().DirectSpaceState;
        var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + (Vector2.Right * MaxLength).Rotated(Rotation), 1);
        var result = spaceState.IntersectRay(query);
        _beamEnd = result.ContainsKey("position")
            ? (Vector2)result["position"]
            : GlobalPosition + (Vector2.Right * MaxLength).Rotated(Rotation);

        if (_line != null)
        {
            _line.SetPointPosition(1, ToLocal(_beamEnd));
        }

        if (_hitArea != null)
        {
            var shape = new CollisionShape2D();
            var length = GlobalPosition.DistanceTo(_beamEnd);
            shape.Shape = new RectangleShape2D { Size = new Vector2(length, 4.0f) };
            shape.Rotation = Rotation;
            shape.Position = new Vector2(length / 2.0f, 0);
            _hitArea.AddChild(shape);
            _hitArea.BodyEntered += OnBodyEntered;
        }

        _timer = BeamDuration;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _timer -= dt;

        if (_timer <= 0)
        {
            QueueFree();
            return;
        }

        if (_hitArea != null)
        {
            _tickTimer -= dt;
            if (_tickTimer <= 0)
            {
                _tickTimer = TickInterval;
                foreach (var body in _hitArea.GetOverlappingBodies())
                {
                    if (body is PlayerController player)
                        player.TakeDamage(DamagePerTick);
                }
            }
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is PlayerController player)
            player.TakeDamage(DamagePerTick);
    }
}
