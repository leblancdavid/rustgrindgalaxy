using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// Bakes sprite textures into black, vertically flipped ground shadows.
/// The squash and sun skew are applied at runtime by Shadow via the sprite
/// transform; only the flip is baked so the feet row stays the anchor.
/// </summary>
public static class ShadowSilhouette
{
    // Entries die with their source texture (scene reloads do not leak).
    private static readonly ConditionalWeakTable<Texture2D, Texture2D> Cache = new();

    public static Texture2D? Get(Texture2D? source)
    {
        if (source == null || !GodotObject.IsInstanceValid(source))
            return null;
        if (Cache.TryGetValue(source, out var baked))
            return baked;
        var result = Bake(source);
        if (result != null)
            Cache.AddOrUpdate(source, result);
        return result;
    }

    private static Texture2D? Bake(Texture2D source)
    {
        var img = source.GetImage();
        if (img == null)
            return null;

        var w = img.GetWidth();
        var h = img.GetHeight();

        var alpha = new float[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                alpha[y * w + x] = img.GetPixel(x, y).A;

        // Flip vertically: the sprite's bottom (feet) becomes the shadow's top
        // row, so the silhouette stays welded at the ground contact point.
        var flipped = new float[w * h];
        for (var y = 0; y < h; y++)
        {
            var sy = h - 1 - y;
            for (var x = 0; x < w; x++)
                flipped[y * w + x] = alpha[sy * w + x];
        }

        var blurred = new float[w * h];
        BoxBlurAlpha(flipped, blurred, w, h);

        var outImg = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                outImg.SetPixel(x, y, new Color(0f, 0f, 0f, blurred[y * w + x]));

        return ImageTexture.CreateFromImage(outImg);
    }

    private static void BoxBlurAlpha(float[] src, float[] dst, int w, int h)
    {
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var sum = 0.0f;
                var n = 0;
                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var sx = x + dx;
                        var sy = y + dy;
                        if (sx < 0 || sy < 0 || sx >= w || sy >= h)
                            continue;
                        sum += src[sy * w + sx];
                        n++;
                    }
                }
                dst[y * w + x] = sum / n;
            }
        }
    }
}
