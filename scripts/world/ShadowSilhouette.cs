using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// Bakes sprite textures and Polygon2D visuals into black, vertically flipped
/// ground shadows. The squash and sun skew are applied at runtime by Shadow
/// via the sprite transform; only the flip is baked so the feet row stays the
/// anchor. Each bake also records where the visual's bottom opaque row sits in
/// the source's local space, so Shadow can start the shadow at the object's
/// true bottom even when the art is sunk into the floor for perspective.
/// </summary>
public static class ShadowSilhouette
{
    public sealed class Bake
    {
        public Texture2D? Texture;

        // Foot segment (bottom opaque row of the art) in the visual's
        // pre-transform local space: midpoint x, y, and half-width. Shadow
        // uses ToGlobal on these to get the object's true ground line even
        // when the visual is rotated (props on ramps) or flipped (west-facing
        // sprites with negative scale).
        public float FootMidLocalX;
        public float FootLocalY;
        public float FootHalfWidth;
    }

    // Entries die with their source resource (scene reloads do not leak).
    private static readonly ConditionalWeakTable<Texture2D, Bake> Cache = new();
    private static readonly ConditionalWeakTable<Polygon2D, Bake> PolygonCache = new();

    private const int MaxBakePixels = 512 * 512;

    public static Bake? Get(Texture2D? source)
    {
        if (source == null || !GodotObject.IsInstanceValid(source))
            return null;
        if (Cache.TryGetValue(source, out var cached))
            return cached;
        var result = BakeTexture(source);
        if (result != null)
            Cache.AddOrUpdate(source, result);
        return result;
    }

    public static Bake? Get(Polygon2D? polygon)
    {
        if (polygon == null || !GodotObject.IsInstanceValid(polygon))
            return null;
        if (PolygonCache.TryGetValue(polygon, out var cached))
            return cached;
        var result = BakePolygon(polygon);
        if (result != null)
            PolygonCache.AddOrUpdate(polygon, result);
        return result;
    }

    private static Bake? BakeTexture(Texture2D source)
    {
        var img = source.GetImage();
        if (img == null)
            return null;

        var w = img.GetWidth();
        var h = img.GetHeight();

        var alpha = new float[w * h];
        var minX = w;
        var minY = h;
        var maxX = -1;
        var maxY = -1;
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var a = img.GetPixel(x, y).A;
                alpha[y * w + x] = a;
                if (a > 0.02f)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }
        if (maxX < minX)
            return null;

        // Flip about the bottommost OPAQUE row, not the canvas edge: sprite art
        // carries transparent padding, and mirroring that padding would detach
        // the shadow from the feet.
        var tex = FinishBake(alpha, w, h, minX, minY, maxX, maxY);
        if (tex == null)
            return null;
        return new Bake
        {
            Texture = tex,
            FootMidLocalX = (minX + maxX) * 0.5f - w * 0.5f,
            FootLocalY = maxY + 0.5f - h * 0.5f,
            FootHalfWidth = (maxX - minX + 1) * 0.5f,
        };
    }

    private static Bake? BakePolygon(Polygon2D polygon)
    {
        var pts = polygon.Polygon;
        if (pts.Length < 3)
            return null;

        var scale = polygon.Scale;
        var tp = new Vector2[pts.Length];
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;
        for (var i = 0; i < pts.Length; i++)
        {
            var p = pts[i] * scale;
            tp[i] = p;
            minX = Mathf.Min(minX, p.X);
            maxX = Mathf.Max(maxX, p.X);
            minY = Mathf.Min(minY, p.Y);
            maxY = Mathf.Max(maxY, p.Y);
        }

        var w = Mathf.Max(1, Mathf.CeilToInt(maxX - minX));
        var h = Mathf.Max(1, Mathf.CeilToInt(maxY - minY));
        if (w * h > MaxBakePixels)
            return null;

        // Even-odd scanline fill; our visuals are convex rects but this stays
        // correct for simple concave shapes too.
        var alpha = new float[w * h];
        var crossings = new System.Collections.Generic.List<float>(8);
        for (var y = 0; y < h; y++)
        {
            var wy = minY + y + 0.5f;
            crossings.Clear();
            for (var i = 0; i < tp.Length; i++)
            {
                var a = tp[i];
                var b = tp[(i + 1) % tp.Length];
                if ((a.Y <= wy && b.Y > wy) || (b.Y <= wy && a.Y > wy))
                {
                    var f = (wy - a.Y) / (b.Y - a.Y);
                    crossings.Add(a.X + f * (b.X - a.X));
                }
            }
            crossings.Sort();
            for (var k = 0; k + 1 < crossings.Count; k += 2)
            {
                var x0 = Mathf.Clamp(Mathf.CeilToInt(crossings[k] - minX), 0, w - 1);
                var x1 = Mathf.Clamp(Mathf.FloorToInt(crossings[k + 1] - minX), 0, w - 1);
                for (var x = x0; x <= x1; x++)
                    alpha[y * w + x] = 1f;
            }
        }

        var tex = FinishBake(alpha, w, h, 0, 0, w - 1, h - 1);
        if (tex == null)
            return null;
        // Canvas holds scale-applied points; ToGlobal re-applies them, so
        // report the foot segment back in pre-scale local space. (Offset is
        // ignored: visual nodes in this project keep it at zero.)
        return new Bake
        {
            Texture = tex,
            FootMidLocalX = scale.X != 0.0f ? (minX + maxX) * 0.5f / scale.X : (minX + maxX) * 0.5f,
            FootLocalY = scale.Y != 0.0f ? maxY / scale.Y : maxY,
            FootHalfWidth = (maxX - minX) * 0.5f / Mathf.Abs(scale.X != 0.0f ? scale.X : 1.0f),
        };
    }

    private static Texture2D? FinishBake(float[] alpha, int w, int h, int bx0, int by0, int bx1, int by1)
    {
        var bw = bx1 - bx0 + 1;
        var bh = by1 - by0 + 1;
        if (bw < 1 || bh < 1 || bw * bh > MaxBakePixels)
            return null;

        // Flip vertically: the region's bottom (feet) becomes the shadow's top
        // row, so the silhouette stays welded at the ground contact point.
        var flipped = new float[bw * bh];
        for (var v = 0; v < bh; v++)
            for (var u = 0; u < bw; u++)
                flipped[v * bw + u] = alpha[(by1 - v) * w + (bx0 + u)];

        var blurred = new float[bw * bh];
        BoxBlurAlpha(flipped, blurred, bw, bh);

        var outImg = Image.CreateEmpty(bw, bh, false, Image.Format.Rgba8);
        for (var y = 0; y < bh; y++)
            for (var x = 0; x < bw; x++)
                outImg.SetPixel(x, y, new Color(0f, 0f, 0f, blurred[y * bw + x]));

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
