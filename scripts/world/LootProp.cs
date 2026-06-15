using System.Collections.Generic;
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
    private Polygon2D _visual;
    private Node2D _glow;
    private MineralType _mineral = MineralType.Cinder;
    private bool _mineralSet;
    private MineralType _primaryMineral = MineralType.Cinder;
    private MineralType _secondaryMineral = MineralType.Azure;
    private bool _shattered;
    private float _groundOffset;

    private struct FragmentState
    {
        public Polygon2D Node;
        public Vector2 Velocity;
        public float RotSpeed;
    }

    private List<FragmentState> _debris;
    private float _shatterElapsed;

    private const float DebrisGravity = 500f;
    private const float ShatterDuration = 1.2f;

    public override void _Ready()
    {
        if (_visual == null)
            BuildChildren();
    }

    public override void _Process(double delta)
    {
        if (_shattered)
        {
            UpdateShatter((float)delta);
            return;
        }

        if (GetWorld2D()?.DirectSpaceState is not PhysicsDirectSpaceState2D space)
            return;

        var rectShape = new RectangleShape2D();
        rectShape.Size = new Vector2(_width, _height);

        var query = new PhysicsShapeQueryParameters2D();
        query.Shape = rectShape;
        query.Transform = new Transform2D(0, GlobalPosition);
        query.CollisionMask = 1;

        foreach (var result in space.IntersectShape(query))
        {
            if (result["collider"].AsGodotObject() is PlayerController player)
            {
                CollectAndShatter(player);
                return;
            }
        }
    }

    public void Initialize(LootType type, float width, float height, int minAmount, int maxAmount, float groundOffset = 5f)
    {
        Type = type;
        _width = width;
        _height = height;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
        _groundOffset = groundOffset;

        BuildChildren();
    }

    private void BuildChildren()
    {
        if (_visual != null)
            return;

        _visual = new Polygon2D();
        BuildRectPolygon(_visual, _width, _height);
        _visual.Color = GetTypeColor();
        AddChild(_visual);

        _glow = RectGlow.CreateGlow(_width + 6f, _height + 6f, ZIndex + 1, new GlowParams
        {
            Color = new Color(1.0f, 0.85f, 0.2f),
            BorderThickness = 4f,
            CornerRadius = 3f,
            PeakAlpha = 0.45f,
        });
        AddChild(_glow);
    }

    public void SetMineral(MineralType mineral)
    {
        _mineral = mineral;
        _mineralSet = true;
    }

    public void SetMinerals(MineralType primary, MineralType secondary)
    {
        _primaryMineral = primary;
        _secondaryMineral = secondary;
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

    private void CollectAndShatter(PlayerController player)
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        var totalAmount = rng.RandiRange(MinAmount, MaxAmount);

        var count = Type switch
        {
            LootType.MineralPatch => rng.RandiRange(3, Mathf.Min(5, totalAmount)),
            LootType.Scrap => rng.RandiRange(1, Mathf.Min(3, totalAmount)),
            _ => rng.RandiRange(2, Mathf.Min(4, totalAmount)),
        };

        var amountPerPickup = Mathf.Max(1, totalAmount / count);

        for (var i = 0; i < count; i++)
        {
            var offset = new Vector2(rng.RandfRange(-12f, 12f), rng.RandfRange(-8f, 8f));
            var type = LootPickupType.Mineral;

            if (Type != LootType.MineralPatch)
            {
                var scrapChance = Type == LootType.Scrap ? 0.7f : 0.35f;
                if (rng.Randf() < scrapChance)
                    type = LootPickupType.Scrap;
            }

            var mineral = MineralType.Cinder;
            if (type == LootPickupType.Mineral)
                mineral = rng.Randf() < 0.6f ? _primaryMineral : _secondaryMineral;

            LootPickup.Spawn(GetParent(), GlobalPosition + offset, player, type, mineral, amountPerPickup);
        }

        PlayShatter();
    }

    private void PlayShatter()
    {
        _shattered = true;
        _visual.Visible = false;
        _glow.Visible = false;

        var rng = new RandomNumberGenerator();
        rng.Randomize();

        var fragmentCount = Type switch
        {
            LootType.Crate => 6,
            LootType.Scrap => 8,
            LootType.MineralPatch => 5,
            _ => 6,
        };

        var baseColor = GetTypeColor();
        var cols = Mathf.CeilToInt(Mathf.Sqrt(fragmentCount));
        var rows = Mathf.CeilToInt((float)fragmentCount / cols);
        var fragW = _width / cols;
        var fragH = _height / rows;

        _debris = new List<FragmentState>(fragmentCount);
        _shatterElapsed = 0f;

        var used = 0;
        for (var r = 0; r < rows && used < fragmentCount; r++)
        {
            for (var c = 0; c < cols && used < fragmentCount; c++)
            {
                var frag = new Polygon2D();
                BuildRectPolygon(frag, fragW, fragH);

                var colorVar = new Color(
                    Mathf.Clamp(baseColor.R + rng.RandfRange(-0.08f, 0.08f), 0f, 1f),
                    Mathf.Clamp(baseColor.G + rng.RandfRange(-0.08f, 0.08f), 0f, 1f),
                    Mathf.Clamp(baseColor.B + rng.RandfRange(-0.08f, 0.08f), 0f, 1f),
                    1f);
                frag.Color = colorVar;
                frag.ZIndex = ZIndex + 2;

                var localX = -_width / 2f + fragW * (c + 0.5f);
                var localY = -_height / 2f + fragH * (r + 0.5f);
                frag.Position = new Vector2(localX, localY);
                AddChild(frag);

                var state = new FragmentState
                {
                    Node = frag,
                    Velocity = new Vector2(
                        rng.RandfRange(-80f, 80f),
                        rng.RandfRange(-200f, -80f)),
                    RotSpeed = rng.RandfRange(-4f, 4f),
                };

                _debris.Add(state);
                used++;
            }
        }
    }

    private void UpdateShatter(float delta)
    {
        _shatterElapsed += delta;
        var floorLocalY = _height / 2f - _groundOffset;

        for (var i = 0; i < _debris.Count; i++)
        {
            var state = _debris[i];
            state.Velocity.Y += DebrisGravity * delta;
            var newPos = state.Node.Position + state.Velocity * delta;

            if (newPos.Y >= floorLocalY)
            {
                newPos.Y = floorLocalY;
                state.Velocity = Vector2.Zero;
            }

            state.Node.Position = newPos;
            state.Node.Rotation += state.RotSpeed * delta;

            if (_shatterElapsed > 0.4f)
            {
                var t = (_shatterElapsed - 0.4f) / 0.4f;
                state.Node.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(1f - t, 0f, 1f));
            }

            _debris[i] = state;
        }

        if (_shatterElapsed >= ShatterDuration)
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
