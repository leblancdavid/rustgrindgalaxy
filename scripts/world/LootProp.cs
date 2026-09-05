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
    private Sprite2D _visual;
    private MineralType _mineral = MineralType.Cinder;
    private bool _mineralSet;
    private MineralType _primaryMineral = MineralType.Cinder;
    private MineralType _secondaryMineral = MineralType.Azure;
    private bool _shattered;
    private float _groundOffset;

    private struct FragmentState
    {
        public Sprite2D Node;
        public Vector2 Velocity;
        public float RotSpeed;
    }

    private List<FragmentState> _debris;
    private float _shatterElapsed;

    private const float DebrisGravity = 500f;
    private const float ShatterDuration = 1.2f;
    private const float PropVisualScale = 1.5f;

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
        rectShape.Size = new Vector2(_width * PropVisualScale, _height * PropVisualScale);

        var query = new PhysicsShapeQueryParameters2D();
        query.Shape = rectShape;
        query.Transform = new Transform2D(0, GlobalPosition);
        query.CollisionMask = 2;

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

        var sprite = Type switch
        {
            LootType.Crate => LootVisuals.PickCrate(),
            LootType.Scrap => LootVisuals.PickPile(),
            LootType.MineralPatch => LootVisuals.PickPatch(),
            _ => default,
        };

        _visual = new Sprite2D();
        _visual.Texture = sprite.Art;
        if (sprite.Art != null)
        {
            _visual.Scale = new Vector2(
                _width * PropVisualScale / sprite.Art.GetWidth(),
                _height * PropVisualScale / sprite.Art.GetHeight());
            LootVisuals.AttachGlow(_visual, sprite);
        }
        _visual.Modulate = GetTypeColor();
        AddChild(_visual);
    }

    public void SetMineral(MineralType mineral)
    {
        _mineral = mineral;
        _mineralSet = true;
        if (_visual != null)
            _visual.Modulate = GetTypeColor();
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
            LootType.Crate => new Color(0.80f, 0.60f, 0.42f),
            LootType.Scrap => new Color(0.92f, 0.88f, 0.82f),
            LootType.MineralPatch => GetMineralPatchColor(),
            _ => Colors.White,
        };
    }

    private Color GetMineralPatchColor()
    {
        if (!_mineralSet)
            return new Color(0.85f, 0.80f, 0.50f);

        return LevelColorPalette.GetMineralLight(_mineral);
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
                var frag = new Sprite2D();
                frag.Texture = LootVisuals.PickScrap().Art;
                var sizeJitter = rng.RandfRange(0.5f, 0.95f) * PropVisualScale;
                if (frag.Texture != null)
                {
                    frag.Scale = new Vector2(
                        fragW * sizeJitter / frag.Texture.GetWidth(),
                        fragH * sizeJitter / frag.Texture.GetHeight());
                }

                var colorVar = new Color(
                    Mathf.Clamp(baseColor.R + rng.RandfRange(-0.08f, 0.08f), 0f, 1f),
                    Mathf.Clamp(baseColor.G + rng.RandfRange(-0.08f, 0.08f), 0f, 1f),
                    Mathf.Clamp(baseColor.B + rng.RandfRange(-0.08f, 0.08f), 0f, 1f),
                    1f);
                frag.Modulate = colorVar;
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
}
