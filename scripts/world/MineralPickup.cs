using Godot;

public partial class MineralPickup : Area2D
{
    [Export] public MineralType Mineral = MineralType.Cinder;
    [Export] public int Amount = 1;

    private Sprite2D _visual = null!;

    public override void _Ready()
    {
        _visual = GetNode<Sprite2D>("Visual");
        var sprite = LootVisuals.PickMineral();
        _visual.Texture = sprite.Art;
        _visual.Scale = Vector2.One * LootVisuals.PickupVisualScale;
        LootVisuals.AttachGlow(_visual, sprite);
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

    private void UpdateVisual()
    {
        if (_visual != null)
        {
            _visual.Modulate = LevelColorPalette.GetMineralLight(Mineral);
        }
    }
}
