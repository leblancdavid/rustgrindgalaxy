using Godot;
using System.Collections.Generic;

public partial class TileLevelGenerator : Node2D
{
    private const string BeaconScenePath = "res://scenes/world/RespawnBeacon.tscn";
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

    private const string RampSectionDescPath = "res://scenes/world/tiles/industrial/RampSectionDescTile.tscn";
    private const string StairClimbDescPath = "res://scenes/world/tiles/industrial/StairClimbDescTile.tscn";
    private const string GentleRiseDescPath = "res://scenes/world/tiles/industrial/GentleRiseDescTile.tscn";
    private const string MidRiseDescPath = "res://scenes/world/tiles/industrial/MidRiseDescTile.tscn";

    private const string CatwalkFlatRunPath = "res://scenes/world/tiles/industrial/CatwalkFlatRunTile.tscn";
    private const string CatwalkHalfPipePath = "res://scenes/world/tiles/industrial/CatwalkHalfPipeTile.tscn";
    private const string CatwalkGapJumpPath = "res://scenes/world/tiles/industrial/CatwalkGapJumpTile.tscn";
    private const string CatwalkMultiLevelPath = "res://scenes/world/tiles/industrial/CatwalkMultiLevelTile.tscn";
    private const string CatwalkRampSectionPath = "res://scenes/world/tiles/industrial/CatwalkRampSectionTile.tscn";
    private const string CatwalkStairClimbPath = "res://scenes/world/tiles/industrial/CatwalkStairClimbTile.tscn";
    private const string CatwalkHighFlatPath = "res://scenes/world/tiles/industrial/CatwalkHighFlatTile.tscn";
    private const string CatwalkGentleRisePath = "res://scenes/world/tiles/industrial/CatwalkGentleRiseTile.tscn";
    private const string CatwalkMidFlatPath = "res://scenes/world/tiles/industrial/CatwalkMidFlatTile.tscn";
    private const string CatwalkMidRisePath = "res://scenes/world/tiles/industrial/CatwalkMidRiseTile.tscn";
    private const string CatwalkRampSectionDescPath = "res://scenes/world/tiles/industrial/CatwalkRampSectionDescTile.tscn";
    private const string CatwalkStairClimbDescPath = "res://scenes/world/tiles/industrial/CatwalkStairClimbDescTile.tscn";
    private const string CatwalkGentleRiseDescPath = "res://scenes/world/tiles/industrial/CatwalkGentleRiseDescTile.tscn";
    private const string CatwalkMidRiseDescPath = "res://scenes/world/tiles/industrial/CatwalkMidRiseDescTile.tscn";

    private const string SteepRampDescPath = "res://scenes/world/tiles/industrial/SteepRampDescTile.tscn";
    private const string SteepRampDesc45Path = "res://scenes/world/tiles/industrial/SteepRampDesc45Tile.tscn";
    private const string SteepRampDesc60Path = "res://scenes/world/tiles/industrial/SteepRampDesc60Tile.tscn";
    private const string SteepRampAscPath = "res://scenes/world/tiles/industrial/SteepRampAscTile.tscn";
    private const string SteepRampAsc45Path = "res://scenes/world/tiles/industrial/SteepRampAsc45Tile.tscn";
    private const string SteepRampAsc60Path = "res://scenes/world/tiles/industrial/SteepRampAsc60Tile.tscn";
    private const string RampGapPath = "res://scenes/world/tiles/industrial/RampGapTile.tscn";
    private const string RailGapPath = "res://scenes/world/tiles/industrial/RailGapTile.tscn";
    private const string RailGapAngledPath = "res://scenes/world/tiles/industrial/RailGapAngledTile.tscn";

    private const string CatwalkSteepRampDescPath = "res://scenes/world/tiles/industrial/CatwalkSteepRampDescTile.tscn";
    private const string CatwalkSteepRampDesc45Path = "res://scenes/world/tiles/industrial/CatwalkSteepRampDesc45Tile.tscn";
    private const string CatwalkSteepRampDesc60Path = "res://scenes/world/tiles/industrial/CatwalkSteepRampDesc60Tile.tscn";
    private const string CatwalkSteepRampAscPath = "res://scenes/world/tiles/industrial/CatwalkSteepRampAscTile.tscn";
    private const string CatwalkSteepRampAsc45Path = "res://scenes/world/tiles/industrial/CatwalkSteepRampAsc45Tile.tscn";
    private const string CatwalkSteepRampAsc60Path = "res://scenes/world/tiles/industrial/CatwalkSteepRampAsc60Tile.tscn";
    private const string CatwalkRampGapPath = "res://scenes/world/tiles/industrial/CatwalkRampGapTile.tscn";
    private const string CatwalkRailGapPath = "res://scenes/world/tiles/industrial/CatwalkRailGapTile.tscn";
    private const string CatwalkRailGapAngledPath = "res://scenes/world/tiles/industrial/CatwalkRailGapAngledTile.tscn";

    [Export] public int MinLevelTiles = 15;
    [Export] public int TilesAheadOfPlayer = 5;
    [Export] public float RemoveBehindDistance = 2560.0f;
    [Export] public int BeaconInterval = 5;
    [Export] public bool CycleAllTilesBeforeRepeat = false;

    private const float TileW = 1280.0f;

    private PlayerController _player = null!;
    private MissionLevel _missionLevel = null!;
    private RandomNumberGenerator _rng = new();
    private PackedScene _beaconScene = null!;
    private readonly List<LevelTile> _activeTiles = new();
    private readonly List<TileEntry> _tilePool = new();
    private bool _levelComplete;
    private bool _started;
    private ExtractionZone _extractionZone = null!;
    private readonly List<int> _cycleQueue = new();
    private int _cycleIndex;
    private LevelColorPalette _colorPalette;

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

    public void Initialize(PlayerController player, MissionLevel level, long seed, LevelColorPalette colorPalette = default)
    {
        _player = player;
        _missionLevel = level;
        _rng = new RandomNumberGenerator { Seed = (ulong)seed };
        _colorPalette = colorPalette;
        // Brightness 0 means the caller passed no palette (struct default);
        // skip so the player dust keeps its plain tint color.
        if (colorPalette.Brightness > 0f)
        {
            _player?.SetLevelPalette(colorPalette);
        }
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

        if (CycleAllTilesBeforeRepeat)
            ShuffleCycleQueue();

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

    private static void SetTerrainCollisionMask(Node node, uint mask)
    {
        if (node is StaticBody2D body)
            body.CollisionMask = mask;
        foreach (var child in node.GetChildren())
            SetTerrainCollisionMask(child, mask);
    }

    private void AppendAndPlaceTile(TileEntry entry, ref float offsetX)
    {
        var tile = entry.Scene.Instantiate<LevelTile>();
        AddChild(tile);

        SetTerrainCollisionMask(tile, 6u);

        tile.FloorSegments = LevelTile.GetDefaultFloorSegments(entry.Name);

        var tileY = _activeTiles.Count > 0
            ? _activeTiles[^1].Position.Y + _activeTiles[^1].RightGroundY - tile.LeftGroundY
            : 0f;

        tile.Position = new Vector2(offsetX, tileY);

		tile.ClearExcludedPositions();
		tile.SpawnInteractiveProps(_rng);
		tile.SpawnFloorProps(_rng, _missionLevel?.GetPropPalette() ?? PropPalettes.Industrial, _colorPalette);
		tile.SpawnLootProps(_rng, _colorPalette);
		tile.SpawnRailSupports();
		tile.ApplyVisualPalette(_colorPalette);

        if (GeneratedTileCount > 0 && GeneratedTileCount % BeaconInterval == BeaconInterval - 1)
            PlaceBeacon(tile);

        _activeTiles.Add(tile);
        GeneratedTileCount++;
        offsetX = tile.GetTileRightX();
        LevelEndX = offsetX;
    }

    private void PlaceBeacon(LevelTile tile)
    {
        var beacon = _beaconScene.Instantiate<RespawnBeacon>();
        AddChild(beacon);
        beacon.SetPlayer(_player);
        var leftX = tile.GetTileLeftX();
        var surfaceY = tile.Position.Y + tile.LeftGroundY;
        beacon.Position = new Vector2(leftX + 40f, surfaceY);

        var beaconGlobal = beacon.ToGlobal(Vector2.Zero);
        var beaconTileLocal = tile.ToLocal(beaconGlobal);
        tile.RemovePropsNear(beaconTileLocal);
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
        if (CycleAllTilesBeforeRepeat)
        {
            if (_cycleIndex >= _cycleQueue.Count)
                ShuffleCycleQueue();
            return _tilePool[_cycleQueue[_cycleIndex++]];
        }

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

    private void ShuffleCycleQueue()
    {
        _cycleQueue.Clear();
        for (var i = 1; i < _tilePool.Count; i++)
            _cycleQueue.Add(i);

        for (var i = _cycleQueue.Count - 1; i > 0; i--)
        {
            var j = _rng.RandiRange(0, i);
            (_cycleQueue[i], _cycleQueue[j]) = (_cycleQueue[j], _cycleQueue[i]);
        }

        _cycleIndex = 0;
    }

    private void LoadTilePool()
    {
        _beaconScene = GD.Load<PackedScene>(BeaconScenePath);
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(FlatRunPath), Name = "FlatRun", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(HalfPipePath), Name = "HalfPipe", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(GapJumpPath), Name = "GapJump", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(MultiLevelPath), Name = "MultiLevel", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(RampSectionPath), Name = "RampSection", LeftGroundY = 164, RightGroundY = 60, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(StairClimbPath), Name = "StairClimb", LeftGroundY = 164, RightGroundY = 60, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(HighFlatPath), Name = "HighFlat", LeftGroundY = 60, RightGroundY = 60, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(GentleRisePath), Name = "GentleRise", LeftGroundY = 164, RightGroundY = 100, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(MidFlatPath), Name = "MidFlat", LeftGroundY = 100, RightGroundY = 100, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(MidRisePath), Name = "MidRise", LeftGroundY = 100, RightGroundY = 60, Weight = 1.0f });

        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkFlatRunPath), Name = "FlatRun", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkHalfPipePath), Name = "HalfPipe", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkGapJumpPath), Name = "GapJump", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkMultiLevelPath), Name = "MultiLevel", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkRampSectionPath), Name = "RampSection", LeftGroundY = 164, RightGroundY = 60, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkStairClimbPath), Name = "StairClimb", LeftGroundY = 164, RightGroundY = 60, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkHighFlatPath), Name = "HighFlat", LeftGroundY = 60, RightGroundY = 60, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkGentleRisePath), Name = "GentleRise", LeftGroundY = 164, RightGroundY = 100, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkMidFlatPath), Name = "MidFlat", LeftGroundY = 100, RightGroundY = 100, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkMidRisePath), Name = "MidRise", LeftGroundY = 100, RightGroundY = 60, Weight = 1.0f });

        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(SteepRampDescPath), Name = "SteepRampDesc", LeftGroundY = 60, RightGroundY = 260, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(SteepRampDesc45Path), Name = "SteepRampDesc45", LeftGroundY = 60, RightGroundY = 360, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(SteepRampDesc60Path), Name = "SteepRampDesc60", LeftGroundY = 60, RightGroundY = 460, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(SteepRampAscPath), Name = "SteepRampAsc", LeftGroundY = 260, RightGroundY = 60, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(SteepRampAsc45Path), Name = "SteepRampAsc45", LeftGroundY = 360, RightGroundY = 60, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(SteepRampAsc60Path), Name = "SteepRampAsc60", LeftGroundY = 460, RightGroundY = 60, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(RampGapPath), Name = "RampGap", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(RailGapPath), Name = "RailGap", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(RailGapAngledPath), Name = "RailGapAngled", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });

        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkSteepRampDescPath), Name = "SteepRampDesc", LeftGroundY = 60, RightGroundY = 260, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkSteepRampDesc45Path), Name = "SteepRampDesc45", LeftGroundY = 60, RightGroundY = 360, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkSteepRampDesc60Path), Name = "SteepRampDesc60", LeftGroundY = 60, RightGroundY = 460, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkSteepRampAscPath), Name = "SteepRampAsc", LeftGroundY = 260, RightGroundY = 60, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkSteepRampAsc45Path), Name = "SteepRampAsc45", LeftGroundY = 360, RightGroundY = 60, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkSteepRampAsc60Path), Name = "SteepRampAsc60", LeftGroundY = 460, RightGroundY = 60, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkRampGapPath), Name = "RampGap", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkRailGapPath), Name = "RailGap", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkRailGapAngledPath), Name = "RailGapAngled", LeftGroundY = 164, RightGroundY = 164, Weight = 1.0f });

        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(RampSectionDescPath), Name = "RampSectionDesc", LeftGroundY = 60, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(StairClimbDescPath), Name = "StairClimbDesc", LeftGroundY = 60, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(GentleRiseDescPath), Name = "GentleRiseDesc", LeftGroundY = 100, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(MidRiseDescPath), Name = "MidRiseDesc", LeftGroundY = 60, RightGroundY = 100, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkRampSectionDescPath), Name = "RampSectionDesc", LeftGroundY = 60, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkStairClimbDescPath), Name = "StairClimbDesc", LeftGroundY = 60, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkGentleRiseDescPath), Name = "GentleRiseDesc", LeftGroundY = 100, RightGroundY = 164, Weight = 1.0f });
        _tilePool.Add(new TileEntry { Scene = GD.Load<PackedScene>(CatwalkMidRiseDescPath), Name = "MidRiseDesc", LeftGroundY = 60, RightGroundY = 100, Weight = 1.0f });
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
