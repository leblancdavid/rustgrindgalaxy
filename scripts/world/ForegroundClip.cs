using Godot;
using System.Collections.Generic;

/// <summary>
/// Registry of foreground occluder polygons in world space (Foreground-layer
/// props). If a shadow caster's ground-contact point is covered by one of
/// these, the caster is drawn behind the foreground art, so its shadow must
/// not leak out from under it: the shadow hides along with the feet.
/// Points are resolved lazily like GroundClip (props position themselves
/// after being added to the tree).
/// </summary>
public static class ForegroundClip
{
    public sealed class Entry
    {
        public Node Owner = null!;
        private Polygon2D? _source;
        private bool _built;
        private Vector2[] _world = System.Array.Empty<Vector2>();

        public void Setup(Node owner, Polygon2D source)
        {
            Owner = owner;
            _source = source;
        }

        private void EnsureBuilt()
        {
            if (_built)
                return;
            _built = true;
            var poly = _source;
            _source = null;
            if (poly == null || !GodotObject.IsInstanceValid(poly) || !poly.IsInsideTree() || !poly.Visible)
                return;
            var pts = poly.Polygon;
            _world = new Vector2[pts.Length];
            for (var i = 0; i < pts.Length; i++)
                _world[i] = poly.ToGlobal(pts[i] * poly.Scale);
        }

        public bool Covers(Vector2 p)
        {
            EnsureBuilt();
            if (_world.Length < 3)
                return false;
            // Even-odd raycast to the left.
            var inside = false;
            for (var i = 0; i < _world.Length; i++)
            {
                var a = _world[i];
                var b = _world[(i + 1) % _world.Length];
                if ((a.Y > p.Y) != (b.Y > p.Y))
                {
                    var t = (p.Y - a.Y) / (b.Y - a.Y);
                    if (p.X < a.X + t * (b.X - a.X))
                        inside = !inside;
                }
            }
            return inside;
        }
    }

    private static readonly List<Entry> Entries = new();

    public static List<Entry> Register(Node owner, Polygon2D visual)
    {
        var e = new Entry();
        e.Setup(owner, visual);
        Entries.Add(e);
        return new List<Entry> { e };
    }

    public static void Remove(List<Entry>? entries)
    {
        if (entries == null)
            return;
        foreach (var e in entries)
            Entries.Remove(e);
        entries.Clear();
    }

    public static bool Covers(Vector2 p, Node? skipOwner)
    {
        for (var i = 0; i < Entries.Count; i++)
        {
            var e = Entries[i];
            if (skipOwner != null && ReferenceEquals(e.Owner, skipOwner))
                continue;
            if (e.Covers(p))
                return true;
        }
        return false;
    }
}
