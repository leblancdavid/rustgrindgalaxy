using Godot;
using System.Collections.Generic;

public abstract partial class MissionLevel : Node2D
{
    public abstract ExtractionZone GetExtractionZone();

    public abstract void ApplyMission(MissionRunData mission);

    public virtual void SpawnLevelProps(RandomNumberGenerator rng)
    {
    }

    public virtual List<PropTemplate> GetPropPalette()
    {
        return PropPalettes.Industrial;
    }
}

public struct PropTemplate
{
    public float Width;
    public float Height;
    public Color Color;
    public bool IsLighting;
    public float Weight;
    public Prop.PropLayer Layer;
    public float GlowYOffset;
    public float GlowScaleX;
    public float GlowScaleY;
}

public static class PropPalettes
{
    public static readonly List<PropTemplate> Industrial = new()
    {
        // Background layer — tall, distant props (behind ground)
        new() { Width = 12, Height = 96, Color = new Color(0.18f, 0.18f, 0.22f), IsLighting = true, Weight = 5f, Layer = Prop.PropLayer.Background, GlowYOffset = -36f, GlowScaleX = 2.5f, GlowScaleY = 0.3f },
        new() { Width = 16, Height = 108, Color = new Color(0.22f, 0.14f, 0.10f), IsLighting = true, Weight = 4f, Layer = Prop.PropLayer.Background, GlowYOffset = -42f, GlowScaleX = 2.2f, GlowScaleY = 0.25f },
        new() { Width = 18, Height = 120, Color = new Color(0.25f, 0.15f, 0.10f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        new() { Width = 96, Height = 18, Color = new Color(0.40f, 0.35f, 0.20f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        new() { Width = 108, Height = 16, Color = new Color(0.35f, 0.30f, 0.25f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        new() { Width = 30, Height = 66, Color = new Color(0.22f, 0.25f, 0.28f), IsLighting = false, Weight = 5f, Layer = Prop.PropLayer.Background },
        new() { Width = 48, Height = 84, Color = new Color(0.28f, 0.30f, 0.35f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        new() { Width = 20, Height = 84, Color = new Color(0.28f, 0.30f, 0.35f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        new() { Width = 10, Height = 72, Color = new Color(0.15f, 0.15f, 0.17f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        // Default layer — mid-ground clutter
        new() { Width = 36, Height = 36, Color = new Color(0.40f, 0.35f, 0.30f), IsLighting = false, Weight = 7f, Layer = Prop.PropLayer.Default },
        new() { Width = 48, Height = 24, Color = new Color(0.30f, 0.32f, 0.35f), IsLighting = false, Weight = 7f, Layer = Prop.PropLayer.Default },
        new() { Width = 24, Height = 48, Color = new Color(0.25f, 0.27f, 0.30f), IsLighting = false, Weight = 6f, Layer = Prop.PropLayer.Default },
        new() { Width = 72, Height = 60, Color = new Color(0.38f, 0.33f, 0.28f), IsLighting = false, Weight = 6f, Layer = Prop.PropLayer.Default },
        new() { Width = 84, Height = 42, Color = new Color(0.42f, 0.38f, 0.32f), IsLighting = false, Weight = 5f, Layer = Prop.PropLayer.Default },
        new() { Width = 24, Height = 24, Color = new Color(0.35f, 0.30f, 0.25f), IsLighting = false, Weight = 5f, Layer = Prop.PropLayer.Default },
        new() { Width = 18, Height = 18, Color = new Color(0.28f, 0.28f, 0.30f), IsLighting = false, Weight = 5f, Layer = Prop.PropLayer.Default },
        new() { Width = 30, Height = 48, Color = new Color(0.55f, 0.40f, 0.25f), IsLighting = false, Weight = 5f, Layer = Prop.PropLayer.Default },
        new() { Width = 42, Height = 20, Color = new Color(0.45f, 0.40f, 0.35f), IsLighting = false, Weight = 5f, Layer = Prop.PropLayer.Default },
        new() { Width = 60, Height = 18, Color = new Color(0.50f, 0.45f, 0.38f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Default },
        new() { Width = 20, Height = 42, Color = new Color(0.60f, 0.35f, 0.20f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Default },
        new() { Width = 18, Height = 36, Color = new Color(0.38f, 0.42f, 0.45f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Default },
        new() { Width = 10, Height = 10, Color = new Color(0.25f, 0.25f, 0.28f), IsLighting = false, Weight = 3f, Layer = Prop.PropLayer.Default },
        new() { Width = 12, Height = 12, Color = new Color(0.75f, 0.70f, 0.30f), IsLighting = false, Weight = 3f, Layer = Prop.PropLayer.Default },
        new() { Width = 12, Height = 12, Color = new Color(0.30f, 0.80f, 0.95f), IsLighting = false, Weight = 3f, Layer = Prop.PropLayer.Default },
        // Foreground layer — props drawn on top of player
        new() { Width = 26, Height = 92, Color = new Color(0.85f, 0.60f, 0.05f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Foreground },
        new() { Width = 80, Height = 32, Color = new Color(0.50f, 0.55f, 0.60f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Foreground },
        new() { Width = 52, Height = 40, Color = new Color(0.55f, 0.40f, 0.25f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Foreground },
        new() { Width = 40, Height = 66, Color = new Color(0.35f, 0.45f, 0.55f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Foreground },
        new() { Width = 66, Height = 26, Color = new Color(0.65f, 0.35f, 0.15f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Foreground },
        new() { Width = 34, Height = 52, Color = new Color(0.30f, 0.60f, 0.25f), IsLighting = false, Weight = 3f, Layer = Prop.PropLayer.Foreground },
        new() { Width = 10, Height = 10, Color = new Color(0.90f, 0.85f, 0.40f), IsLighting = false, Weight = 3f, Layer = Prop.PropLayer.Foreground },
        new() { Width = 14, Height = 14, Color = new Color(0.95f, 0.30f, 0.25f), IsLighting = false, Weight = 3f, Layer = Prop.PropLayer.Foreground },
    };

    public static readonly List<PropTemplate> Derelict = new()
    {
        // Background — rusted structures
        new() { Width = 12, Height = 90, Color = new Color(0.30f, 0.20f, 0.15f), IsLighting = true, Weight = 5f, Layer = Prop.PropLayer.Background, GlowYOffset = -35f, GlowScaleX = 2.5f, GlowScaleY = 0.25f },
        new() { Width = 24, Height = 108, Color = new Color(0.25f, 0.18f, 0.12f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        new() { Width = 72, Height = 20, Color = new Color(0.35f, 0.22f, 0.15f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        new() { Width = 12, Height = 60, Color = new Color(0.20f, 0.15f, 0.10f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        new() { Width = 36, Height = 54, Color = new Color(0.40f, 0.28f, 0.18f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        // Default — derelict clutter
        new() { Width = 54, Height = 28, Color = new Color(0.28f, 0.20f, 0.14f), IsLighting = false, Weight = 6f, Layer = Prop.PropLayer.Default },
        new() { Width = 36, Height = 30, Color = new Color(0.45f, 0.30f, 0.20f), IsLighting = false, Weight = 6f, Layer = Prop.PropLayer.Default },
        new() { Width = 24, Height = 24, Color = new Color(0.40f, 0.28f, 0.18f), IsLighting = false, Weight = 5f, Layer = Prop.PropLayer.Default },
        new() { Width = 18, Height = 18, Color = new Color(0.32f, 0.22f, 0.15f), IsLighting = false, Weight = 5f, Layer = Prop.PropLayer.Default },
        new() { Width = 20, Height = 42, Color = new Color(0.35f, 0.25f, 0.16f), IsLighting = false, Weight = 5f, Layer = Prop.PropLayer.Default },
        new() { Width = 12, Height = 12, Color = new Color(0.22f, 0.16f, 0.10f), IsLighting = false, Weight = 3f, Layer = Prop.PropLayer.Default },
        new() { Width = 10, Height = 10, Color = new Color(0.60f, 0.50f, 0.20f), IsLighting = false, Weight = 3f, Layer = Prop.PropLayer.Default },
        // Foreground
        new() { Width = 66, Height = 26, Color = new Color(0.50f, 0.35f, 0.22f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Foreground },
        new() { Width = 34, Height = 60, Color = new Color(0.55f, 0.30f, 0.18f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Foreground },
        new() { Width = 46, Height = 32, Color = new Color(0.45f, 0.28f, 0.15f), IsLighting = false, Weight = 3f, Layer = Prop.PropLayer.Foreground },
        new() { Width = 20, Height = 16, Color = new Color(0.80f, 0.60f, 0.30f), IsLighting = false, Weight = 3f, Layer = Prop.PropLayer.Foreground },
    };

    public static readonly List<PropTemplate> Surface = new()
    {
        // Background — distant rock formations and crystal pillars
        new() { Width = 12, Height = 84, Color = new Color(0.20f, 0.22f, 0.25f), IsLighting = true, Weight = 5f, Layer = Prop.PropLayer.Background, GlowYOffset = -32f, GlowScaleX = 3f, GlowScaleY = 0.3f },
        new() { Width = 24, Height = 84, Color = new Color(0.25f, 0.22f, 0.18f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        new() { Width = 72, Height = 20, Color = new Color(0.35f, 0.30f, 0.22f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        new() { Width = 16, Height = 72, Color = new Color(0.20f, 0.18f, 0.15f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        new() { Width = 42, Height = 48, Color = new Color(0.38f, 0.32f, 0.24f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Background },
        // Default — rocky ground clutter
        new() { Width = 30, Height = 28, Color = new Color(0.45f, 0.38f, 0.30f), IsLighting = false, Weight = 7f, Layer = Prop.PropLayer.Default },
        new() { Width = 48, Height = 20, Color = new Color(0.35f, 0.30f, 0.24f), IsLighting = false, Weight = 7f, Layer = Prop.PropLayer.Default },
        new() { Width = 60, Height = 30, Color = new Color(0.42f, 0.35f, 0.26f), IsLighting = false, Weight = 6f, Layer = Prop.PropLayer.Default },
        new() { Width = 24, Height = 20, Color = new Color(0.40f, 0.35f, 0.28f), IsLighting = false, Weight = 5f, Layer = Prop.PropLayer.Default },
        new() { Width = 18, Height = 16, Color = new Color(0.32f, 0.28f, 0.22f), IsLighting = false, Weight = 5f, Layer = Prop.PropLayer.Default },
        new() { Width = 20, Height = 30, Color = new Color(0.28f, 0.24f, 0.18f), IsLighting = false, Weight = 5f, Layer = Prop.PropLayer.Default },
        new() { Width = 12, Height = 12, Color = new Color(0.50f, 0.45f, 0.35f), IsLighting = false, Weight = 3f, Layer = Prop.PropLayer.Default },
        new() { Width = 10, Height = 10, Color = new Color(0.60f, 0.55f, 0.30f), IsLighting = false, Weight = 3f, Layer = Prop.PropLayer.Default },
        // Foreground — surface debris
        new() { Width = 52, Height = 32, Color = new Color(0.35f, 0.40f, 0.22f), IsLighting = false, Weight = 5f, Layer = Prop.PropLayer.Foreground },
        new() { Width = 40, Height = 26, Color = new Color(0.50f, 0.42f, 0.30f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Foreground },
        new() { Width = 30, Height = 46, Color = new Color(0.40f, 0.30f, 0.20f), IsLighting = false, Weight = 4f, Layer = Prop.PropLayer.Foreground },
        new() { Width = 20, Height = 16, Color = new Color(0.70f, 0.65f, 0.30f), IsLighting = false, Weight = 3f, Layer = Prop.PropLayer.Foreground },
    };
}
