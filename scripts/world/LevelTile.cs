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
    private static readonly float MaxRampAngleRad = Mathf.Pi / 6f; // 30 degrees
    private readonly List<Vector2> _excludedPositions = new();
    private LevelColorPalette _colorPalette;

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

    public float GetTileLeftX() => Position.X;

    public float GetTileRightX() => Position.X + TileWidth;

    public void SpawnLootProps(RandomNumberGenerator rng, LevelColorPalette colorPalette, float chance = 0.5f, int minCount = 1, int maxCount = 3)
    {
        if (FloorSegments == null || FloorSegments.Length == 0)
            return;

        if (rng.Randf() >= chance)
            return;

        var count = rng.RandiRange(minCount, maxCount);
        for (var i = 0; i < count; i++)
        {
            var segment = PickSegment(FloorSegments, rng);
            if (segment == null)
                continue;

            var roll = rng.Randf();
            var type = roll < 0.40f ? LootType.Crate : roll < 0.75f ? LootType.Scrap : LootType.MineralPatch;

            float width, height;
            int minAmount, maxAmount;
            switch (type)
            {
                case LootType.Crate:
                    width = rng.RandfRange(28f, 36f);
                    height = rng.RandfRange(22f, 30f);
                    minAmount = 1;
                    maxAmount = 3;
                    break;
                case LootType.Scrap:
                    width = rng.RandfRange(32f, 48f);
                    height = rng.RandfRange(14f, 20f);
                    minAmount = 1;
                    maxAmount = 2;
                    break;
                default:
                    width = rng.RandfRange(16f, 24f);
                    height = rng.RandfRange(12f, 18f);
                    minAmount = 2;
                    maxAmount = 4;
                    break;
            }

            var halfWidth = width * 0.5f + 2f;
            var minX = segment.Value.StartX + halfWidth;
            var maxX = segment.Value.EndX - halfWidth;
            if (minX >= maxX)
                continue;

            var localX = rng.RandfRange(minX, maxX);
            var t = Mathf.InverseLerp(segment.Value.StartX, segment.Value.EndX, localX);
            var floorY = Mathf.Lerp(segment.Value.StartY, segment.Value.EndY, t);

            var segDx = segment.Value.EndX - segment.Value.StartX;
            var segDy = segment.Value.EndY - segment.Value.StartY;
            var segmentAngle = Mathf.Atan2(segDy, segDx);
            if (Mathf.Abs(segmentAngle) > MaxRampAngleRad)
                continue;

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

            var loot = new LootProp();

            var baseSink = 5f;
            var slope = segDx != 0f ? Mathf.Abs(segDy / segDx) : 0f;
            var rampSink = slope * width * 0.5f;
            var groundOffset = baseSink + rampSink;
            loot.Initialize(type, width, height, minAmount, maxAmount, groundOffset);

            if (_colorPalette.Brightness > 0f)
            {
                var primaryMineral = ResolvePrimaryMineral(_colorPalette);
                var secondaryMineral = ResolveSecondaryMineral(_colorPalette);
                loot.SetMinerals(primaryMineral, secondaryMineral);

                if (type == LootType.MineralPatch)
                    loot.SetMineral(primaryMineral);
            }

            AddChild(loot);
            loot.Position = new Vector2(localX, floorY - height / 2f + groundOffset);
            loot.Rotation = segmentAngle;
            AddExcludedPosition(new Vector2(localX, floorY));
        }
    }

    private static MineralType ResolvePrimaryMineral(LevelColorPalette palette)
    {
        return ResolveMineralFromColor(palette.PrimaryLight);
    }

    private static MineralType ResolveSecondaryMineral(LevelColorPalette palette)
    {
        return ResolveMineralFromColor(palette.SecondaryLight);
    }

    private static MineralType ResolveMineralFromColor(Color color)
    {
        if (color.G > color.R && color.G > color.B)
            return MineralType.Verdant;
        if (color.B > color.R && color.B > color.G)
            return MineralType.Azure;
        if (color.R > 0.8f && color.G > 0.7f)
            return MineralType.Solar;
        if (color.R > 0.7f && color.G > 0.3f && color.G < 0.6f)
            return MineralType.Cinder;
        if (color.R > 0.8f && color.G > 0.8f && color.B > 0.8f)
            return MineralType.Lumen;
        return MineralType.Umbra;
    }

    public void SpawnFloorProps(RandomNumberGenerator rng, List<PropTemplate> palette, LevelColorPalette colorPalette = default)
    {
        _colorPalette = colorPalette;
        if (FloorSegments == null || FloorSegments.Length == 0 || palette == null || palette.Count == 0)
            return;

        var count = rng.RandiRange(MinProps, MaxProps);
        for (var i = 0; i < count; i++)
        {
            var segment = PickSegment(FloorSegments, rng);
            if (segment == null)
                continue;

            var template = PickPropTemplate(palette, rng);
            var halfWidth = template.Width * 0.5f + 2f;
            var minX = segment.Value.StartX + halfWidth;
            var maxX = segment.Value.EndX - halfWidth;
            if (minX >= maxX)
                continue;

            var localX = rng.RandfRange(minX, maxX);
            var t = Mathf.InverseLerp(segment.Value.StartX, segment.Value.EndX, localX);
            var floorY = Mathf.Lerp(segment.Value.StartY, segment.Value.EndY, t);

            var segDx = segment.Value.EndX - segment.Value.StartX;
            var segDy = segment.Value.EndY - segment.Value.StartY;
            var segmentAngle = Mathf.Atan2(segDy, segDx);
            if (Mathf.Abs(segmentAngle) > MaxRampAngleRad)
                continue;

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

            var visual = new Prop();
            visual.Layer = template.Layer;
            AddChild(visual);
            visual.Initialize(template.Width, template.Height, template.Color, template.IsLighting, template.Layer, template.Slot, template.GlowYOffset, template.GlowScaleX, template.GlowScaleY);
            visual.ApplyPalette(colorPalette);
            var baseSink = 5f;
            var slope = segDx != 0f ? Mathf.Abs(segDy / segDx) : 0f;
            var rampSink = slope * template.Width * 0.5f;
            var fgExtra = template.Layer == Prop.PropLayer.Foreground ? 3f : 0f;
            var groundOffset = baseSink + rampSink + fgExtra;
            visual.Position = new Vector2(localX, floorY - template.Height / 2f + groundOffset);
            visual.Rotation = segmentAngle;
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

    public void ApplyVisualPalette(LevelColorPalette palette)
    {
        var b = palette.Brightness;
        foreach (var child in GetChildren())
        {
            if (child is GrindRail rail)
            {
                rail.ApplyPalette(palette, PaletteSlot.PrimaryLight);
                continue;
            }
            if (child is Polygon2D poly)
            {
                Color color;
                string name = child.Name;
                if (name.Contains("Rise"))
                    color = new Color(palette.SecondaryMedium.R * b, palette.SecondaryMedium.G * b, palette.SecondaryMedium.B * b, 1f);
                else if (name.Contains("Edge"))
                    color = new Color(palette.PrimaryLight.R * b, palette.PrimaryLight.G * b, palette.PrimaryLight.B * b, 1f);
                else if (name.Contains("Trim"))
                    color = new Color(palette.SecondaryLight.R * b, palette.SecondaryLight.G * b, palette.SecondaryLight.B * b, 1f);
                else if (name == "UpperPlatformVisual")
                    color = new Color(palette.SecondaryDark.R * b, palette.SecondaryDark.G * b, palette.SecondaryDark.B * b, 1f);
                else if (name.Contains("Visual"))
                    color = new Color(palette.SecondaryDark.R * b, palette.SecondaryDark.G * b, palette.SecondaryDark.B * b, 1f);
                else
                    continue;
                poly.Color = color;
            }
        }
    }

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
        support.Initialize(RailSupportWidth, height, RailSupportColor, false, Prop.PropLayer.Background);
        support.ApplyPalette(_colorPalette);
        support.Position = new Vector2(localPoint.X, midY);
        AddChild(support);
    }

    public float GetGroundYAt(float localX)
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
		if (Mathf.Abs(angle) > MaxRampAngleRad)
			return;

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
		if (Mathf.Abs(angle) > MaxRampAngleRad)
			return;

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

		if (Mathf.Abs(rail.Angle) > MaxRampAngleRad)
			return;

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
			if (child is BoostPad boostPad)
			{
				var dist = boostPad.Position.DistanceTo(localPoint);
				if (dist < PropExclusionRadius)
					boostPad.QueueFree();
			}
			if (child is LaunchPad launchPad)
			{
				var dist = launchPad.Position.DistanceTo(localPoint);
				if (dist < PropExclusionRadius)
					launchPad.QueueFree();
			}
			if (child is GrindBoost grindBoost)
			{
				var dist = grindBoost.Position.DistanceTo(localPoint);
				if (dist < PropExclusionRadius)
					grindBoost.QueueFree();
			}
			if (child is LootProp lootProp)
			{
				var dist = lootProp.Position.DistanceTo(localPoint);
				if (dist < PropExclusionRadius)
					lootProp.QueueFree();
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

        ["RampSectionDesc"] = new[] {
            new FloorSegment(0, 320, 60, 60),
            new FloorSegment(320, 960, 60, 164),
            new FloorSegment(960, 1280, 164, 164),
        },
        ["StairClimbDesc"] = new[] {
            new FloorSegment(0, 160, 60, 60),
            new FloorSegment(160, 320, 60, 60),
            new FloorSegment(320, 480, 79, 79),
            new FloorSegment(480, 640, 96, 96),
            new FloorSegment(640, 800, 113, 113),
            new FloorSegment(800, 960, 130, 130),
            new FloorSegment(960, 1120, 147, 147),
            new FloorSegment(1120, 1280, 164, 164),
        },
        ["GentleRiseDesc"] = new[] {
            new FloorSegment(0, 320, 100, 100),
            new FloorSegment(320, 960, 100, 164),
            new FloorSegment(960, 1280, 164, 164),
        },
        ["MidRiseDesc"] = new[] {
            new FloorSegment(0, 320, 60, 60),
            new FloorSegment(320, 960, 60, 100),
            new FloorSegment(960, 1280, 100, 100),
        },

        ["SteepRampDesc"] = new[] {
            new FloorSegment(0, 440, 60, 60),
            new FloorSegment(440, 500, 60, 81.84f),
            new FloorSegment(500, 656, 81.84f, 238.16f),
            new FloorSegment(656, 716, 238.16f, 260),
            new FloorSegment(716, 1280, 260, 260),
        },
        ["SteepRampAsc"] = new[] {
            new FloorSegment(0, 440, 260, 260),
            new FloorSegment(440, 500, 260, 238.16f),
            new FloorSegment(500, 656, 238.16f, 81.84f),
            new FloorSegment(656, 716, 81.84f, 60),
            new FloorSegment(716, 1280, 60, 60),
        },
        ["SteepRampDesc45"] = new[] {
            new FloorSegment(0, 440, 60, 60),
            new FloorSegment(440, 500, 60, 81.84f),
            new FloorSegment(500, 756, 81.84f, 338.16f),
            new FloorSegment(756, 816, 338.16f, 360),
            new FloorSegment(816, 1280, 360, 360),
        },
        ["SteepRampAsc45"] = new[] {
            new FloorSegment(0, 440, 360, 360),
            new FloorSegment(440, 500, 360, 338.16f),
            new FloorSegment(500, 756, 338.16f, 81.84f),
            new FloorSegment(756, 816, 81.84f, 60),
            new FloorSegment(816, 1280, 60, 60),
        },
        ["SteepRampDesc60"] = new[] {
            new FloorSegment(0, 460, 60, 60),
            new FloorSegment(460, 520, 60, 81.89f),
            new FloorSegment(520, 570, 81.89f, 131.89f),
            new FloorSegment(570, 718, 131.89f, 388.16f),
            new FloorSegment(718, 768, 388.16f, 438.16f),
            new FloorSegment(768, 828, 438.16f, 460),
            new FloorSegment(828, 1280, 460, 460),
        },
        ["SteepRampAsc60"] = new[] {
            new FloorSegment(0, 460, 460, 460),
            new FloorSegment(460, 520, 460, 438.16f),
            new FloorSegment(520, 570, 438.16f, 388.16f),
            new FloorSegment(570, 718, 388.16f, 131.89f),
            new FloorSegment(718, 768, 131.89f, 81.89f),
            new FloorSegment(768, 828, 81.89f, 60.05f),
            new FloorSegment(828, 1280, 60.05f, 60.05f),
        },
        ["RampGap"] = new[] {
            new FloorSegment(0, 320, 164, 108),
            new FloorSegment(640, 960, 108, 164),
            new FloorSegment(960, 1280, 164, 164),
        },
        ["RailGap"] = new[] {
            new FloorSegment(0, 400, 164, 164),
            new FloorSegment(880, 1280, 164, 164),
        },
        ["RailGapAngled"] = new[] {
            new FloorSegment(0, 400, 164, 164),
            new FloorSegment(880, 1280, 164, 164),
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
