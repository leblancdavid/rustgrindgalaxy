using Godot;

public partial class MineralPickup : Area2D
{
    [Export] public MineralType Mineral = MineralType.Cinder;
    [Export] public int Amount = 1;

    private Polygon2D _visual = null!;

    public override void _Ready()
    {
        _visual = GetNode<Polygon2D>("Visual");
        var glow = RectGlow.CreateGlow(18f, 18f, ZIndex + 1);
        AddChild(glow);
        UpdateVisual();
        BodyEntered += OnBodyEntered;
    }

    public void SetMineral(MineralType mineral)
    {
        Mineral = mineral;
        UpdateVisual();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not PlayerController player)
        {
            return;
        }

        var world = player.GetParentOrNull<World>();
        world?.CollectMineral(Mineral, Amount);
        QueueFree();
    }

    private static Color GetMineralColor(MineralType mineral)
    {
        return mineral switch
        {
            MineralType.Cinder => new Color(0.9098f, 0.3804f, 0.2627f, 1.0f),
            MineralType.Verdant => new Color(0.3725f, 0.7569f, 0.4039f, 1.0f),
            MineralType.Azure => new Color(0.3569f, 0.6745f, 0.9451f, 1.0f),
            MineralType.Solar => new Color(0.9686f, 0.8078f, 0.2706f, 1.0f),
            MineralType.Lumen => new Color(0.9255f, 0.9412f, 0.9804f, 1.0f),
            MineralType.Umbra => new Color(0.3216f, 0.2745f, 0.4078f, 1.0f),
            _ => Colors.White,
        };
    }

    private void UpdateVisual()
    {
        if (_visual != null)
        {
            _visual.Color = GetMineralColor(Mineral);
        }
    }
}
