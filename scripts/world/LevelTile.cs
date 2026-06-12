using Godot;
using System.Collections.Generic;

public partial class LevelTile : Node2D
{
    [Export] public float TileWidth = 1280.0f;
    [Export] public float LeftGroundY = 164.0f;
    [Export] public float LeftRailY = -1.0f;
    [Export] public float RightGroundY = 164.0f;
    [Export] public float RightRailY = -1.0f;

    public FloorSegment[] FloorSegments;
    public int MinProps = 4;
    public int MaxProps = 8;

    private const float PropExclusionRadius = 60.0f;
    private readonly List<Vector2> _excludedPositions = new();

    public void ClearExcludedPositions() => _excludedPositions.Clear();

    public void AddExcludedPosition(Vector2 pos) => _excludedPositions.Add(pos);

    public LevelTileConnector GetLeftConnector()
    {
        return new LevelTileConnector { GroundY = LeftGroundY, RailY = LeftRailY };
    }

    public LevelTileConnector GetRightConnector()
    {
        return new LevelTileConnector { GroundY = RightGroundY, RailY = RightRailY };
    }

    public float GetTileLeftX()
    {
        return Scale.X < 0 ? Position.X - TileWidth : Position.X;
    }

    public float GetTileRightX()
    {
        return Scale.X < 0 ? Position.X : Position.X + TileWidth;
    }

    public void SpawnFloorProps(RandomNumberGenerator rng, List<PropTemplate> palette)
    {
        if (FloorSegments == null || FloorSegments.Length == 0 || palette == null || palette.Count == 0)
            return;

        var count = rng.RandiRange(MinProps, MaxProps);
        for (var i = 0; i < count; i++)
        {
            var segment = PickSegment(FloorSegments, rng);
            if (segment == null)
                continue;

            var localX = rng.RandfRange(segment.Value.StartX, segment.Value.EndX);
            var t = Mathf.InverseLerp(segment.Value.StartX, segment.Value.EndX, localX);
            var floorY = Mathf.Lerp(segment.Value.StartY, segment.Value.EndY, t);

            var isExcluded = false;
            foreach (var ep in _excludedPositions)
            {
                var dx = localX - ep.X;
                var dy = floorY - ep.Y;
                if (dx * dx + dy * dy < PropExclusionRadius * PropExclusionRadius)
                {
                    isExcluded = true;
                    break;
                }
            }
            if (isExcluded)
                continue;

            var template = PickPropTemplate(palette, rng);
            var visual = new Prop();
            visual.Layer = template.Layer;
            AddChild(visual);
            visual.Initialize(template.Width, template.Height, template.Color, template.IsLighting, template.Layer, template.GlowYOffset, template.GlowScaleX, template.GlowScaleY);
            var baseSink = 5f;
            var slope = segment.Value.EndX != segment.Value.StartX
                ? Mathf.Abs((segment.Value.EndY - segment.Value.StartY) / (segment.Value.EndX - segment.Value.StartX))
                : 0f;
            var rampSink = slope * template.Width * 0.5f;
            var fgExtra = template.Layer == Prop.PropLayer.Foreground ? 3f : 0f;
            var groundOffset = baseSink + rampSink + fgExtra;
            visual.Position = new Vector2(localX, floorY - template.Height / 2f + groundOffset);
        }
    }

    private static FloorSegment? PickSegment(FloorSegment[] segments, RandomNumberGenerator rng)
    {
        var totalLength = 0.0f;
        foreach (var seg in segments)
            totalLength += seg.EndX - seg.StartX;

        if (totalLength <= 0)
            return null;

        var roll = rng.Randf() * totalLength;
        foreach (var seg in segments)
        {
            var len = seg.EndX - seg.StartX;
            roll -= len;
            if (roll <= 0)
                return seg;
        }

        return segments[^1];
    }

    private static readonly Color RailSupportColor = new Color(0.35f, 0.35f, 0.38f);
    private const float RailSupportWidth = 6.0f;

    public void SpawnRailSupports()
    {
        foreach (var child in GetChildren())
        {
            if (child is not GrindRail rail)
                continue;

            SpawnSupportAt(rail.StartPoint);
            SpawnSupportAt(rail.EndPoint);
        }
    }

    private void SpawnSupportAt(Vector2 globalPoint)
    {
        var localPoint = ToLocal(globalPoint);
        var groundY = GetGroundYAt(localPoint.X);
        if (groundY < 0f)
            return;

        var height = groundY - localPoint.Y;
        if (height <= 1f)
            return;

        var midY = (localPoint.Y + groundY) * 0.5f;
        var support = new Prop();
        support.Initialize(RailSupportWidth, height, RailSupportColor, false);
        support.Position = new Vector2(localPoint.X, midY);
        AddChild(support);
    }

    private float GetGroundYAt(float localX)
    {
        if (FloorSegments == null || FloorSegments.Length == 0)
            return -1f;

        foreach (var seg in FloorSegments)
        {
            if (localX >= seg.StartX && localX <= seg.EndX)
            {
                var t = Mathf.InverseLerp(seg.StartX, seg.EndX, localX);
                return Mathf.Lerp(seg.StartY, seg.EndY, t);
            }
        }

        return -1f;
    }

    private static PropTemplate PickPropTemplate(List<PropTemplate> palette, RandomNumberGenerator rng)
    {
        var totalWeight = 0.0f;
        foreach (var t in palette)
            totalWeight += t.Weight;

        var roll = rng.Randf() * totalWeight;
        foreach (var t in palette)
        {
            roll -= t.Weight;
            if (roll <= 0)
                return t;
        }

        return palette[^1];
    }

	public void SpawnInteractiveProps(RandomNumberGenerator rng, float chance = 0.3f)
	{
		if (rng.Randf() >= chance)
			return;

		var roll = rng.RandiRange(0, 2);

		if (roll == 0)
			SpawnBoostPad(rng);
		else if (roll == 1)
			SpawnLaunchPad(rng);
		else
			SpawnGrindBoost(rng);
	}

	private void SpawnBoostPad(RandomNumberGenerator rng)
	{
		var segment = PickSegment(FloorSegments, rng);
		if (segment == null)
			return;

		var localX = rng.RandfRange(segment.Value.StartX + 24f, segment.Value.EndX - 24f);
		if (localX <= segment.Value.StartX)
			localX = segment.Value.StartX + 24f;

		var t = Mathf.InverseLerp(segment.Value.StartX, segment.Value.EndX, localX);
		var floorY = Mathf.Lerp(segment.Value.StartY, segment.Value.EndY, t);

		var dx = segment.Value.EndX - segment.Value.StartX;
		var dy = segment.Value.EndY - segment.Value.StartY;
		var angle = Mathf.Atan2(dy, dx);

		var pad = new BoostPad();
		AddChild(pad);
		pad.Position = new Vector2(localX, floorY);
		pad.Rotation = angle;
		AddExcludedPosition(new Vector2(localX, floorY));
	}

	private void SpawnLaunchPad(RandomNumberGenerator rng)
	{
		var segment = PickSegment(FloorSegments, rng);
		if (segment == null)
			return;

		var localX = rng.RandfRange(segment.Value.StartX + 24f, segment.Value.EndX - 24f);
		if (localX <= segment.Value.StartX)
			localX = segment.Value.StartX + 24f;

		var t = Mathf.InverseLerp(segment.Value.StartX, segment.Value.EndX, localX);
		var floorY = Mathf.Lerp(segment.Value.StartY, segment.Value.EndY, t);

		var dx = segment.Value.EndX - segment.Value.StartX;
		var dy = segment.Value.EndY - segment.Value.StartY;
		var angle = Mathf.Atan2(dy, dx);

		var pad = new LaunchPad();
		AddChild(pad);
		pad.Position = new Vector2(localX, floorY);
		pad.Rotation = angle;
		AddExcludedPosition(new Vector2(localX, floorY));
	}

	private void SpawnGrindBoost(RandomNumberGenerator rng)
	{
		GrindRail rail = null;
		foreach (var child in GetChildren())
		{
			if (child is GrindRail candidate)
			{
				rail = candidate;
				break;
			}
		}

		if (rail == null)
		{
			SpawnBoostPad(rng);
			return;
		}

		var localRailCenter = ToLocal(rail.StartPoint.Lerp(rail.EndPoint, 0.5f));

		var pad = new GrindBoost();
		AddChild(pad);
		pad.Position = new Vector2(localRailCenter.X, localRailCenter.Y);
		pad.Rotation = rail.Angle;
		AddExcludedPosition(localRailCenter);
	}

	public void RemovePropsNear(Vector2 localPoint)
	{
		foreach (var child in GetChildren())
		{
			if (child is Prop prop)
			{
				var dist = prop.Position.DistanceTo(localPoint);
				if (dist < PropExclusionRadius)
					prop.QueueFree();
			}
		}
	}

	public static FloorSegment[] GetDefaultFloorSegments(string tileName)
    {
        return _defaultFloorSegments.TryGetValue(tileName, out var segments) ? segments : null;
    }

    private static readonly Dictionary<string, FloorSegment[]> _defaultFloorSegments = new()
    {
        ["FlatRun"] = new[] { new FloorSegment(0, 1280, 164, 164) },
        ["HalfPipe"] = new[] {
            new FloorSegment(0, 320, 164, 164),
            new FloorSegment(320, 480, 164, 184),
            new FloorSegment(480, 800, 184, 184),
            new FloorSegment(800, 960, 184, 164),
            new FloorSegment(960, 1280, 164, 164),
        },
        ["GapJump"] = new[] {
            new FloorSegment(0, 560, 164, 164),
            new FloorSegment(720, 1280, 164, 164),
        },
        ["MultiLevel"] = new[] {
            new FloorSegment(0, 320, 164, 164),
            new FloorSegment(320, 960, 105, 105),
            new FloorSegment(960, 1280, 164, 164),
        },
        ["RampSection"] = new[] {
            new FloorSegment(0, 320, 164, 164),
            new FloorSegment(320, 960, 164, 60),
            new FloorSegment(960, 1280, 60, 60),
        },
        ["StairClimb"] = new[] {
            new FloorSegment(0, 160, 164, 164),
            new FloorSegment(160, 320, 147, 147),
            new FloorSegment(320, 480, 130, 130),
            new FloorSegment(480, 640, 113, 113),
            new FloorSegment(640, 800, 96, 96),
            new FloorSegment(800, 960, 79, 79),
            new FloorSegment(960, 1120, 60, 60),
            new FloorSegment(1120, 1280, 60, 60),
        },
        ["HighFlat"] = new[] { new FloorSegment(0, 1280, 60, 60) },
        ["GentleRise"] = new[] {
            new FloorSegment(0, 320, 164, 164),
            new FloorSegment(320, 960, 164, 100),
            new FloorSegment(960, 1280, 100, 100),
        },
        ["MidFlat"] = new[] { new FloorSegment(0, 1280, 100, 100) },
        ["MidRise"] = new[] {
            new FloorSegment(0, 320, 100, 100),
            new FloorSegment(320, 960, 100, 60),
            new FloorSegment(960, 1280, 60, 60),
        },
    };

}

public struct FloorSegment
{
    public float StartX;
    public float EndX;
    public float StartY;
    public float EndY;

    public FloorSegment(float startX, float endX, float startY, float endY)
    {
        StartX = startX;
        EndX = endX;
        StartY = startY;
        EndY = endY;
    }
}
