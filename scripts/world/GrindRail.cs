using Godot;

public partial class GrindRail : Node2D
{
    [Export] public float Width = 96.0f;
    [Export] public float Height = 10.0f;
    [Export] public float BaseSpeed = 150.0f;
    [Export] public float SnapDistanceAbove = 18.0f;
    [Export] public float SnapDistanceBelow = 5.0f;

    private Area2D _area = null!;
    private CollisionShape2D _collisionShape = null!;
    private Line2D _line = null!;

    public Vector2 StartPoint => ToGlobal(new Vector2(-Width * 0.5f, 0.0f));

    public Vector2 EndPoint => ToGlobal(new Vector2(Width * 0.5f, 0.0f));

    public Vector2 Tangent => (EndPoint - StartPoint).Normalized();

    public float Length => Width;

    public float Angle => Tangent.Angle();

    public override void _Ready()
    {
        _area = GetNode<Area2D>("Area2D");
        _collisionShape = _area.GetNode<CollisionShape2D>("CollisionShape2D");
        _line = GetNode<Line2D>("Line2D");

        UpdateVisuals();
        _area.BodyEntered += OnBodyEntered;
        _area.BodyExited += OnBodyExited;
    }

    private void UpdateVisuals()
    {
        if (_collisionShape.Shape is RectangleShape2D rectangle)
        {
            rectangle.Size = new Vector2(Width, Height);
        }

        _line.Points = new[]
        {
            new Vector2(-Width * 0.5f, 0.0f),
            new Vector2(Width * 0.5f, 0.0f),
        };
    }

    public bool CanSnap(PlayerController player)
    {
        return TryGetSnapProgress(player.GlobalPosition, out _);
    }

    public float GetSpeed(float railSpeedBonus)
    {
        return BaseSpeed + railSpeedBonus;
    }

    public float GetDistanceToPoint(Vector2 point)
    {
        return Mathf.Abs(ToLocal(point).Y);
    }

    public Vector2 GetLocalPoint(Vector2 point)
    {
        return ToLocal(point);
    }

    public bool TryGetSnapProgress(Vector2 point, out float progress)
    {
        var localPoint = ToLocal(point);
        var minX = -Width * 0.5f;
        var maxX = Width * 0.5f;
        var allowedDistance = localPoint.Y <= 0.0f ? SnapDistanceAbove : SnapDistanceBelow;

        progress = Mathf.InverseLerp(minX, maxX, Mathf.Clamp(localPoint.X, minX, maxX));
        return localPoint.X >= minX && localPoint.X <= maxX && Mathf.Abs(localPoint.Y) <= allowedDistance;
    }

    public Vector2 GetPointAtProgress(float progress)
    {
        return StartPoint.Lerp(EndPoint, Mathf.Clamp(progress, 0.0f, 1.0f));
    }

    public float GetProgressAtPoint(Vector2 point)
    {
        var localPoint = ToLocal(point);
        return Mathf.InverseLerp(-Width * 0.5f, Width * 0.5f, Mathf.Clamp(localPoint.X, -Width * 0.5f, Width * 0.5f));
    }

    public float GetDownhillSign()
    {
        return Mathf.Sign(Tangent.Dot(Vector2.Down));
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is PlayerController player)
        {
            player.SetNearbyRail(this);
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is PlayerController player)
        {
            player.ClearNearbyRail(this);
        }
    }
}
