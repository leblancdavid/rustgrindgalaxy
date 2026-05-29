using Godot;

public partial class GrindRail : Node2D
{
    [Export] public float Width = 96.0f;
    [Export] public float Height = 10.0f;
    [Export] public float BaseSpeed = 150.0f;

    private Area2D _area = null!;
    private CollisionShape2D _collisionShape = null!;
    private Line2D _line = null!;

    public float LeftX => GlobalPosition.X - (Width * 0.5f);

    public float RightX => GlobalPosition.X + (Width * 0.5f);

    public float RailY => GlobalPosition.Y;

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
        return player.GlobalPosition.X >= LeftX && player.GlobalPosition.X <= RightX;
    }

    public float GetSpeed(float railSpeedBonus)
    {
        return BaseSpeed + railSpeedBonus;
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
