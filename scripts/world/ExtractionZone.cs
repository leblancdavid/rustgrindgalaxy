using Godot;

public partial class ExtractionZone : Area2D
{
    private Polygon2D _visual = null!;

    public override void _Ready()
    {
        _visual = GetNode<Polygon2D>("Visual");
        BodyEntered += OnBodyEntered;
    }

    public void SetActive(bool isActive)
    {
        SetDeferred(Area2D.PropertyName.Monitoring, isActive);
        SetDeferred(Area2D.PropertyName.Monitorable, isActive);
        _visual.Color = isActive
            ? new Color(0.3176f, 0.9608f, 0.702f, 0.9f)
            : new Color(0.2902f, 0.302f, 0.3608f, 0.55f);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not PlayerController player)
        {
            return;
        }

        var world = player.GetParentOrNull<World>();
        world?.TryCompleteMission();
    }
}
