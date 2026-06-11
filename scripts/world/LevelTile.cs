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

            var template = PickPropTemplate(palette, rng);
            var prop = new Node2D();
            var visual = new Prop();
            prop.AddChild(visual);
            AddChild(prop);
            visual.Initialize(template.Width, template.Height, template.Color, template.IsLighting, template.Layer, template.GlowYOffset, template.GlowScaleX, template.GlowScaleY);
            var baseSink = 3f;
            var slope = segment.Value.EndX != segment.Value.StartX
                ? Mathf.Abs((segment.Value.EndY - segment.Value.StartY) / (segment.Value.EndX - segment.Value.StartX))
                : 0f;
            var rampSink = slope * template.Width * 0.5f;
            var fgExtra = template.Layer == Prop.PropLayer.Foreground ? 9f : 0f;
            var groundOffset = baseSink + rampSink + fgExtra;
            prop.Position = new Vector2(localX, floorY - template.Height / 2f + groundOffset);
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
            new FloorSegment(0, 320, 164, 164),
            new FloorSegment(960, 1280, 164, 164),
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
        ["GentleRise"] = new[] { new FloorSegment(0, 1280, 164, 100) },
        ["MidFlat"] = new[] { new FloorSegment(0, 1280, 100, 100) },
        ["MidRise"] = new[] { new FloorSegment(0, 1280, 100, 60) },
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
