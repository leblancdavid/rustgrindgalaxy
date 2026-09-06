using Godot;
using System.Collections.Generic;

/// <summary>
/// Registry of opaque ground-fill polygons in world space. Tile ground art is
/// defined by the direct Polygon2D children of each LevelTile root (Visual /
/// Trim / EdgeRise pieces; props, rails and bodies are other node types or
/// deeper children, so they never register). Shadows clip to this fill so they
/// only ever appear on non-transparent ground pixels, wherever those are.
/// Points are resolved lazily on first query because tiles position themselves
/// right after being added to the tree.
/// </summary>
public static class GroundClip
{
	public sealed class Entry
	{
		public int Id;
		public Polygon2D? Source;
		private bool _built;
		private Vector2[] _world = System.Array.Empty<Vector2>();
		public float MinX, MinY, MaxX, MaxY, CenterX, CenterY;

		public Vector2[] World => _built ? _world : System.Array.Empty<Vector2>();

		public void EnsureBuilt()
		{
			if (_built)
				return;
			// One-shot: ground tiles never move after placement. If the source
			// is already gone (streamed out between query passes) bake empty.
			_built = true;
			var poly = Source;
			Source = null;
			if (poly == null || !GodotObject.IsInstanceValid(poly) || !poly.IsInsideTree() || !poly.Visible)
				return;

			var pts = poly.Polygon;
			_world = new Vector2[pts.Length];
			MinX = MinY = float.MaxValue;
			MaxX = MaxY = float.MinValue;
			// Ground art uses position only (no node rotation); scale/offset
			// are applied defensively.
			for (var i = 0; i < pts.Length; i++)
			{
				var w = poly.ToGlobal(pts[i] * poly.Scale + poly.Offset);
				_world[i] = w;
				if (w.X < MinX) MinX = w.X;
				if (w.X > MaxX) MaxX = w.X;
				if (w.Y < MinY) MinY = w.Y;
				if (w.Y > MaxY) MaxY = w.Y;
			}
			CenterX = (MinX + MaxX) * 0.5f;
			CenterY = (MinY + MaxY) * 0.5f;
		}
	}

	private static readonly List<Entry> Entries = new();
	private static int _nextId = 1;

	public static List<Entry> Collect(Node2D tileRoot)
	{
		var added = new List<Entry>();
		foreach (var child in tileRoot.GetChildren())
		{
			if (child is Polygon2D poly && poly.Polygon.Length >= 3)
			{
				var e = new Entry { Id = _nextId++, Source = poly };
				Entries.Add(e);
				added.Add(e);
			}
		}
		return added;
	}

	public static void Remove(List<Entry>? entries)
	{
		if (entries == null)
			return;
		foreach (var e in entries)
			Entries.Remove(e);
		entries.Clear();
	}

	public static void Query(Rect2 area, List<Entry> result)
	{
		result.Clear();
		foreach (var e in Entries)
		{
			e.EnsureBuilt();
			if (e.World.Length < 3)
				continue;
			if (e.MaxX >= area.Position.X && e.MinX <= area.End.X
				&& e.MaxY >= area.Position.Y && e.MinY <= area.End.Y)
			{
				result.Add(e);
			}
		}
	}
}
