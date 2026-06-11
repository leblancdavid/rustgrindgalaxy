using Godot;

public partial class Prop : Node2D
{
    public enum PropLayer { Background, Default, Foreground }

    [Export] public float PropWidth = 16.0f;
    [Export] public float PropHeight = 16.0f;
    [Export] public Color PropColor = Colors.White;
    [Export] public bool IsLighting;
    [Export] public PropLayer Layer = PropLayer.Default;
    [Export] public float GlowYOffset;
    [Export] public float GlowScaleX;
    [Export] public float GlowScaleY;

    private Polygon2D _visual;
    private Polygon2D _glow;

    public override void _Ready()
    {
        CreateVisual();
    }

    public void Initialize(float width, float height, Color color, bool lighting, PropLayer layer = PropLayer.Default, float glowYOffset = 0f, float glowScaleX = 0f, float glowScaleY = 0f)
    {
        PropWidth = width;
        PropHeight = height;
        PropColor = color;
        IsLighting = lighting;
        Layer = layer;
        GlowYOffset = glowYOffset;
        GlowScaleX = glowScaleX;
        GlowScaleY = glowScaleY;

        if (_visual == null)
            CreateVisual();
        else
            UpdateVisual();
    }

    private static float ResolveGlowScale(float scale) => scale > 0f ? scale : 1.8f;

    private void CreateVisual()
    {
        ZIndex = Layer switch
        {
            PropLayer.Background => -2,
            PropLayer.Foreground => 4,
            _ => 0,
        };

        _visual = new Polygon2D();
        BuildRectPolygon(_visual, PropWidth, PropHeight);
        _visual.Color = PropColor;
        AddChild(_visual);

        if (IsLighting)
        {
            _glow = new Polygon2D();
            BuildRectPolygon(_glow, PropWidth * ResolveGlowScale(GlowScaleX), PropHeight * ResolveGlowScale(GlowScaleY));
            _glow.Color = new Color(PropColor.R, PropColor.G, PropColor.B, 0.3f);
            _glow.ZIndex = -1;
            _glow.Position = new Vector2(0, GlowYOffset);
            AddChild(_glow);
        }
    }

    private void UpdateVisual()
    {
        ZIndex = Layer switch
        {
            PropLayer.Background => -2,
            PropLayer.Foreground => 4,
            _ => 0,
        };

        BuildRectPolygon(_visual, PropWidth, PropHeight);
        _visual.Color = PropColor;

        if (IsLighting)
        {
            if (_glow == null)
            {
                _glow = new Polygon2D();
                _glow.ZIndex = -1;
                AddChild(_glow);
            }
            BuildRectPolygon(_glow, PropWidth * ResolveGlowScale(GlowScaleX), PropHeight * ResolveGlowScale(GlowScaleY));
            _glow.Color = new Color(PropColor.R, PropColor.G, PropColor.B, 0.3f);
            _glow.Position = new Vector2(0, GlowYOffset);
        }
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
