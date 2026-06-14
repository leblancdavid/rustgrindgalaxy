using Godot;

public enum LootType
{
    Crate,
    Scrap,
    MineralPatch,
}

public partial class LootProp : Area2D
{
    [Export] public LootType Type = LootType.Crate;
    [Export] public int MinAmount = 1;
    [Export] public int MaxAmount = 3;

    private float _width = 30f;
    private float _height = 24f;
    private Polygon2D _visual = null!;
    private MineralType _mineral = MineralType.Cinder;
    private bool _mineralSet;

    public override void _Ready()
    {
        ZIndex = 0;

        var collision = new CollisionShape2D();
        var shape = new RectangleShape2D();
        shape.Size = new Vector2(_width, _height);
        collision.Shape = shape;
        AddChild(collision);

        _visual = new Polygon2D();
        BuildRectPolygon(_visual, _width, _height);
        _visual.Color = GetTypeColor();
        AddChild(_visual);

        var glow = RectGlow.CreateGlow(_width + 6f, _height + 6f, ZIndex + 1, new GlowParams
        {
            Color = new Color(1.0f, 0.85f, 0.2f),
            BorderThickness = 4f,
            CornerRadius = 3f,
            PeakAlpha = 0.45f,
        });
        AddChild(glow);

        BodyEntered += OnBodyEntered;
    }

    public void Initialize(LootType type, float width, float height, int minAmount, int maxAmount)
    {
        Type = type;
        _width = width;
        _height = height;
        MinAmount = minAmount;
        MaxAmount = maxAmount;

        if (_visual != null)
        {
            BuildRectPolygon(_visual, _width, _height);
            _visual.Color = GetTypeColor();
        }
    }

    public void SetMineral(MineralType mineral)
    {
        _mineral = mineral;
        _mineralSet = true;
    }

    private Color GetTypeColor()
    {
        return Type switch
        {
            LootType.Crate => new Color(0.45f, 0.35f, 0.22f),
            LootType.Scrap => new Color(0.50f, 0.48f, 0.45f),
            LootType.MineralPatch => GetMineralPatchColor(),
            _ => Colors.White,
        };
    }

    private Color GetMineralPatchColor()
    {
        if (!_mineralSet)
            return new Color(0.60f, 0.55f, 0.30f);

        return _mineral switch
        {
            MineralType.Cinder => new Color(0.9098f, 0.3804f, 0.2627f),
            MineralType.Verdant => new Color(0.3725f, 0.7569f, 0.4039f),
            MineralType.Azure => new Color(0.3569f, 0.6745f, 0.9451f),
            MineralType.Solar => new Color(0.9686f, 0.8078f, 0.2706f),
            MineralType.Lumen => new Color(0.9255f, 0.9412f, 0.9804f),
            MineralType.Umbra => new Color(0.3216f, 0.2745f, 0.4078f),
            _ => new Color(0.60f, 0.55f, 0.30f),
        };
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not PlayerController player)
            return;

        var world = player.GetParentOrNull<World>();
        if (world == null)
            return;

        var rng = new RandomNumberGenerator();
        rng.Randomize();
        var amount = rng.RandiRange(MinAmount, MaxAmount);

        if (Type == LootType.MineralPatch && _mineralSet)
        {
            world.CollectMineral(_mineral, amount);
        }
        else
        {
            var mineralRoll = rng.RandiRange(0, 5);
            var mineral = (MineralType)mineralRoll;
            world.CollectMineral(mineral, amount);
        }

        QueueFree();
    }

    private static void BuildRectPolygon(Polygon2D poly, float w, float h)
    {
        var hw = w / 2f;
        var hh = h / 2f;
        poly.Polygon = new Vector2[]
        {
            new Vector2(-hw, -hh),
            new Vector2(hw, -hh),
            new Vector2(hw, hh),
            new Vector2(-hw, hh),
        };
    }
}
