using System.Collections.Generic;
using Godot;

public static class LootVisuals
{
    public const float PickupVisualScale = 0.75f;

    private const float GlowBaseScale = 0.25f; // glow textures are 4x the art
    private const float GlowStrength = 0.6f;

    public readonly struct LootSprite
    {
        public LootSprite(Texture2D art, Texture2D glow)
        {
            Art = art;
            Glow = glow;
        }

        public Texture2D Art { get; }
        public Texture2D Glow { get; }
    }

    private static readonly LootSprite[] Minerals = LoadPool("res://assets/props/minerals/", "mineral_");
    private static readonly LootSprite[] Scrap = LoadPool("res://assets/props/scrap/", "scrap_");
    private static readonly LootSprite[] Piles = LoadPool("res://assets/props/piles/", "pile_");
    private static readonly LootSprite[] Crates = LoadPool("res://assets/props/crates/", "crate_");
    private static readonly LootSprite[] Patches = LoadPool("res://assets/props/patches/", "patch_");

    public static LootSprite PickMineral() => Pick(Minerals);
    public static LootSprite PickScrap() => Pick(Scrap);
    public static LootSprite PickPile() => Pick(Piles);
    public static LootSprite PickCrate() => Pick(Crates);
    public static LootSprite PickPatch() => Pick(Patches);

    public static void AttachGlow(Sprite2D visual, LootSprite sprite)
    {
        if (sprite.Glow == null)
            return;

        var mat = new CanvasItemMaterial();
        mat.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        var glow = new Sprite2D
        {
            Name = "LootGlow",
            Texture = sprite.Glow,
            Material = mat,
            Scale = new Vector2(GlowBaseScale, GlowBaseScale),
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            ShowBehindParent = true,
        };
        glow.SelfModulate = new Color(1f, 1f, 1f, GlowStrength);
        visual.AddChild(glow);
    }

    private static LootSprite Pick(LootSprite[] pool)
    {
        if (pool.Length == 0)
            return default;
        return pool[(int)(GD.Randi() % (uint)pool.Length)];
    }

    private static LootSprite[] LoadPool(string dir, string prefix)
    {
        var list = new List<LootSprite>();
        for (var i = 0; i < 32; i++)
        {
            var artPath = $"{dir}{prefix}{i:D2}.png";
            if (!ResourceLoader.Exists(artPath))
                continue;
            var art = GD.Load<Texture2D>(artPath);
            if (art == null)
                continue;
            var glowPath = $"{dir}glow/{prefix}{i:D2}.png";
            var glow = ResourceLoader.Exists(glowPath) ? GD.Load<Texture2D>(glowPath) : null;
            list.Add(new LootSprite(art, glow));
        }
        return list.ToArray();
    }
}
