using Godot;

public partial class GrindRail : Node2D
{
    public const string RailGroupName = "grind_rails";

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

    public float Angle => Mathf.Atan2(Tangent.Y, Mathf.Abs(Tangent.X));

    public override void _Ready()
    {
        AddToGroup(RailGroupName);
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

    public bool TryGetSweepSnap(Vector2 fromPoint, Vector2 toPoint, out float progress)
    {
        if (TryGetSnapProgress(toPoint, out progress))
        {
            return true;
        }

        if (TryGetSnapProgress(fromPoint, out progress))
        {
            return true;
        }

        var localFrom = ToLocal(fromPoint);
        var localTo = ToLocal(toPoint);
        var minX = -Width * 0.5f;
        var maxX = Width * 0.5f;
        var minY = -SnapDistanceAbove;
        var maxY = SnapDistanceBelow;

        if (TryClipSegmentToRect(localFrom, localTo, minX, maxX, minY, maxY, out var entryT) == false)
        {
            progress = 0.0f;
            return false;
        }

        var snapPoint = localFrom.Lerp(localTo, entryT);
        progress = Mathf.InverseLerp(minX, maxX, Mathf.Clamp(snapPoint.X, minX, maxX));
        return true;
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

    private static bool TryClipSegmentToRect(Vector2 from, Vector2 to, float minX, float maxX, float minY, float maxY, out float entryT)
    {
        entryT = 0.0f;
        var exitT = 1.0f;
        var delta = to - from;

        if (ClipToBoundary(-delta.X, from.X - minX, ref entryT, ref exitT) == false)
        {
            return false;
        }

        if (ClipToBoundary(delta.X, maxX - from.X, ref entryT, ref exitT) == false)
        {
            return false;
        }

        if (ClipToBoundary(-delta.Y, from.Y - minY, ref entryT, ref exitT) == false)
        {
            return false;
        }

        if (ClipToBoundary(delta.Y, maxY - from.Y, ref entryT, ref exitT) == false)
        {
            return false;
        }

        return entryT <= exitT;
    }

    private static bool ClipToBoundary(float p, float q, ref float entryT, ref float exitT)
    {
        if (Mathf.IsZeroApprox(p))
        {
            return q >= 0.0f;
        }

        var ratio = q / p;

        if (p < 0.0f)
        {
            if (ratio > exitT)
            {
                return false;
            }

            if (ratio > entryT)
            {
                entryT = ratio;
            }

            return true;
        }

        if (ratio < entryT)
        {
            return false;
        }

        if (ratio < exitT)
        {
            exitT = ratio;
        }

        return true;
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
