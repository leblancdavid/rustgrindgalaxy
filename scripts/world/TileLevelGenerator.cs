using Godot;
using System.Collections.Generic;

public partial class TileLevelGenerator : Node2D
{
    private const string FlatRunPath = "res://scenes/world/tiles/industrial/FlatRunTile.tscn";
    private const string RampSectionPath = "res://scenes/world/tiles/industrial/RampSectionTile.tscn";
    private const string StairClimbPath = "res://scenes/world/tiles/industrial/StairClimbTile.tscn";
    private const string HalfPipePath = "res://scenes/world/tiles/industrial/HalfPipeTile.tscn";
    private const string GapJumpPath = "res://scenes/world/tiles/industrial/GapJumpTile.tscn";
    private const string MultiLevelPath = "res://scenes/world/tiles/industrial/MultiLevelTile.tscn";
    private const string HighFlatPath = "res://scenes/world/tiles/industrial/HighFlatTile.tscn";

    private const string GentleRisePath = "res://scenes/world/tiles/industrial/GentleRiseTile.tscn";
    private const string MidFlatPath = "res://scenes/world/tiles/industrial/MidFlatTile.tscn";
    private const string MidRisePath = "res://scenes/world/tiles/industrial/MidRiseTile.tscn";

    [Export] public int MinLevelTiles = 15;
    [Export] public int TilesAheadOfPlayer = 5;
    [Export] public float RemoveBehindDistance = 2560.0f;

    private const float TileW = 1280.0f;

    private PlayerController _player = null!;
    private MissionLevel _missionLevel = null!;
    private RandomNumberGenerator _rng = new();
    private readonly List<LevelTile> _activeTiles = new();
    private readonly List<TileEntry> _tilePool = new();
    private bool _levelComplete;
    private bool _started;
    private ExtractionZone _extractionZone = null!;

    public float LevelEndX { get; private set; }
    public int GeneratedTileCount { get; private set; }

    public IReadOnlyList<LevelTile> ActiveTiles => _activeTiles;

    private struct TileEntry
    {
        public PackedScene Scene;
        public string Name;
        public float LeftGroundY;
        public float RightGroundY;
        public float Weight;
        public FloorSegment[] FloorSegments;
    }

    public void Initialize(PlayerController player, MissionLevel level, long seed)
    {
        _player = player;
        _missionLevel = level;
        _rng = new RandomNumberGenerator { Seed = (ulong)seed };
        LoadTilePool();
    }

    public void SetExtractionZone(ExtractionZone zone)
    {
        _extractionZone = zone;
    }

    public void BuildInitial(float worldOffsetX = 0.0f)
    {
        _activeTiles.Clear();
        GeneratedTileCount = 0;
        _levelComplete = false;
        _started = true;

        foreach (var child in GetChildren())
        {
            if (child is LevelTile)
                child.QueueFree();
        }

        var offsetX = worldOffsetX;
        AppendAndPlaceTile(PickStartEntry(), ref offsetX);

        for (var i = 0; i < TilesAheadOfPlayer; i++)
        {
            var nextEntry = PickNextEntry();
            if (nextEntry != null)
                AppendAndPlaceTile(nextEntry.Value, ref offsetX);
        }
    }

    public void UpdateStreaming()
    {
        if (!_started || _levelComplete || _player == null)
            return;

        var playerX = _player.GlobalPosition.X;
        var frontierX = _activeTiles.Count > 0
            ? _activeTiles[^1].GetTileRightX()
            : 0.0f;

        while (frontierX - playerX < TilesAheadOfPlayer * TileW && !_levelComplete)
        {
            var offsetX = _activeTiles.Count > 0
                ? _activeTiles[^1].GetTileRightX()
                : 0.0f;

            var nextEntry = PickNextEntry();
            if (nextEntry == null)
            {
                _levelComplete = true;
                break;
            }

            if (GeneratedTileCount >= MinLevelTiles)
            {
                var cappedEntry = PickEndCapEntry();
                AppendAndPlaceTile(cappedEntry, ref offsetX);
                PlaceExtractionZone(offsetX);
                _levelComplete = true;
                break;
            }

            AppendAndPlaceTile(nextEntry.Value, ref offsetX);
            frontierX = offsetX;
        }

    }

    public void ClearAllTiles()
    {
        foreach (var child in GetChildren())
        {
            if (child is LevelTile)
                child.QueueFree();
        }

        _activeTiles.Clear();
        GeneratedTileCount = 0;
        _started = false;
    }

    private void AppendAndPlaceTile(TileEntry entry, ref float offsetX)
    {
        var tile = entry.Scene.Instantiate<LevelTile>();
        AddChild(tile);

        tile.FloorSegments = LevelTile.GetDefaultFloorSegments(entry.Name);

        var shouldMirror = _activeTiles.Count > 0 && _rng.Randf() < 0.5f;
        if (shouldMirror)
        {
            tile.Scale = new Vector2(-1, 1);

            var temp = tile.LeftGroundY;
            tile.LeftGroundY = tile.RightGroundY;
            tile.RightGroundY = temp;

            var tempRail = tile.LeftRailY;
            tile.LeftRailY = tile.RightRailY;
            tile.RightRailY = tempRail;
        }

        var tileY = _activeTiles.Count > 0
            ? _activeTiles[^1].Position.Y + _activeTiles[^1].RightGroundY - tile.LeftGroundY
            : 0f;

        tile.Position = new Vector2(offsetX + (shouldMirror ? tile.TileWidth : 0f), tileY);

        tile.SpawnFloorProps(_rng, _missionLevel?.GetPropPalette() ?? PropPalettes.Industrial);
        tile.SpawnRailSupports();
        _activeTiles.Add(tile);
        GeneratedTileCount++;
        offsetX = tile.GetTileRightX();
        LevelEndX = offsetX;
    }

    private void PlaceExtractionZone(float offsetX)
    {
        if (_extractionZone != null && _activeTiles.Count > 0)
        {
            var lastTile = _activeTiles[^1];
            var surfaceY = lastTile.Position.Y + lastTile.RightGroundY;
            _extractionZone.Position = new Vector2(offsetX - 20.0f, surfaceY - 84.0f);
        }
    }

    private TileEntry PickStartEntry()
    {
        return _tilePool[0];
    }

    private TileEntry PickEndCapEntry()
    {
        return _tilePool[0];
    }

    private TileEntry? PickNextEntry()
    {
        var totalWeight = 0.0f;
        foreach (var entry in _tilePool)
            totalWeight += entry.Weight;

        var roll = _rng.Randf() * totalWeight;
        foreach (var entry in _tilePool)
        {
            roll -= entry.Weight;
            if (roll <= 0.0f)
                return entry;
        }

        return _tilePool[^1];
    }

    private void LoadTilePool()
    {
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(FlatRunPath), Name = "FlatRun", LeftGroundY = 164, RightGroundY = 164, Weight = 0.0941f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(HalfPipePath), Name = "HalfPipe", LeftGroundY = 164, RightGroundY = 164, Weight = 0.1412f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(GapJumpPath), Name = "GapJump", LeftGroundY = 164, RightGroundY = 164, Weight = 0.0588f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(MultiLevelPath), Name = "MultiLevel", LeftGroundY = 164, RightGroundY = 164, Weight = 0.1176f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(RampSectionPath), Name = "RampSection", LeftGroundY = 164, RightGroundY = 60, Weight = 0.1882f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(StairClimbPath), Name = "StairClimb", LeftGroundY = 164, RightGroundY = 60, Weight = 0.1647f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(HighFlatPath), Name = "HighFlat", LeftGroundY = 60, RightGroundY = 60, Weight = 0.0824f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(GentleRisePath), Name = "GentleRise", LeftGroundY = 164, RightGroundY = 100, Weight = 0.0706f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(MidFlatPath), Name = "MidFlat", LeftGroundY = 100, RightGroundY = 100, Weight = 0.0471f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(MidRisePath), Name = "MidRise", LeftGroundY = 100, RightGroundY = 60, Weight = 0.0353f });
    }

    public void CollectRails(List<GrindRail> rails)
    {
        rails.Clear();
        foreach (var tile in _activeTiles)
        {
            foreach (var child in tile.GetChildren())
            {
                if (child is GrindRail rail)
                    rails.Add(rail);
            }
        }
    }

    public void CollectSpawnMarkers(string prefix, List<Marker2D> target)
    {
        target.Clear();
        foreach (var tile in _activeTiles)
        {
            CollectMarkerChildren(tile, prefix, target);
        }
    }

    private static void CollectMarkerChildren(Node node, string path, List<Marker2D> target)
    {
        var markerParent = node.GetNodeOrNull<Node>(path);
        if (markerParent == null)
            return;

        foreach (var child in markerParent.GetChildren())
        {
            if (child is Marker2D marker)
                target.Add(marker);
        }
    }
}
