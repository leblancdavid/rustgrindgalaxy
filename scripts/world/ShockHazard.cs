using Godot;

public partial class ShockHazard : Area2D
{
    [Export] public int ContactDamage = 1;

    private Polygon2D _visual = null!;

    public override void _Ready()
    {
        _visual = GetNode<Polygon2D>("Visual");
        BodyEntered += OnBodyEntered;
    }

    public void SetTheme(string paletteKey)
    {
        _visual.Color = paletteKey switch
        {
            "rocky" => new Color(0.839f, 0.564f, 0.203f, 0.95f),
            "frozen" => new Color(0.505f, 0.839f, 0.98f, 0.95f),
            "derelict" => new Color(0.733f, 0.423f, 0.858f, 0.95f),
            _ => new Color(0.984f, 0.776f, 0.243f, 0.95f),
        };
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is PlayerController player)
        {
            player.TakeDamage(ContactDamage);
        }
    }
}
