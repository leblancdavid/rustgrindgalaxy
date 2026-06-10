using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TileLevelGenerator : Node2D
{
    private const string FlatRunPath = "res://scenes/world/tiles/industrial/FlatRunTile.tscn";
    private const string RampSectionPath = "res://scenes/world/tiles/industrial/RampSectionTile.tscn";
    private const string StairClimbPath = "res://scenes/world/tiles/industrial/StairClimbTile.tscn";
    private const string DropSectionPath = "res://scenes/world/tiles/industrial/DropSectionTile.tscn";
    private const string HalfPipePath = "res://scenes/world/tiles/industrial/HalfPipeTile.tscn";
    private const string GapJumpPath = "res://scenes/world/tiles/industrial/GapJumpTile.tscn";
    private const string MultiLevelPath = "res://scenes/world/tiles/industrial/MultiLevelTile.tscn";
    private const string HighFlatPath = "res://scenes/world/tiles/industrial/HighFlatTile.tscn";

    [Export] public int MinLevelTiles = 15;
    [Export] public int TilesAheadOfPlayer = 5;
    [Export] public float RemoveBehindDistance = 2560.0f;

    private const float TileW = 1280.0f;
    private const float GroundY = 164.0f;
    private const float HighY = 60.0f;

    private PlayerController _player = null!;
    private RandomNumberGenerator _rng = new();
    private readonly List<LevelTile> _activeTiles = new();
    private readonly List<PackedScene> _groundTiles = new();
    private readonly List<PackedScene> _risingTiles = new();
    private readonly List<PackedScene> _descendingTiles = new();
    private readonly List<PackedScene> _highTiles = new();
    private bool _levelComplete;
    private bool _started;
    private ExtractionZone _extractionZone = null!;

    public float LevelEndX { get; private set; }
    public int GeneratedTileCount { get; private set; }

    public IReadOnlyList<LevelTile> ActiveTiles => _activeTiles;

    public void Initialize(PlayerController player, long seed)
    {
        _player = player;
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
        AppendAndPlaceTile(PickStartTile(), ref offsetX);

        for (var i = 0; i < TilesAheadOfPlayer; i++)
        {
            var nextTile = PickNextTile();
            if (nextTile != null)
                AppendAndPlaceTile(nextTile, ref offsetX);
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

            var nextTile = PickNextTile();
            if (nextTile == null)
            {
                _levelComplete = true;
                break;
            }

            if (GeneratedTileCount >= MinLevelTiles)
            {
                var cappedTile = PickEndCapTile();
                if (cappedTile != null)
                {
                    AppendAndPlaceTile(cappedTile, ref offsetX);
                    PlaceExtractionZone(offsetX);
                }
                _levelComplete = true;
                break;
            }

            AppendAndPlaceTile(nextTile, ref offsetX);
            frontierX = offsetX;
        }

        RemoveTilesBehind(playerX);
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

    private void AppendAndPlaceTile(PackedScene tileScene, ref float offsetX)
    {
        var tile = tileScene.Instantiate<LevelTile>();
        AddChild(tile);
        tile.Position = new Vector2(offsetX, 0.0f);
        _activeTiles.Add(tile);
        GeneratedTileCount++;
        offsetX = tile.GetTileRightX();
        LevelEndX = offsetX;
    }

    private void PlaceExtractionZone(float offsetX)
    {
        if (_extractionZone != null)
        {
            _extractionZone.Position = new Vector2(offsetX - 20.0f, 80.0f);
        }
    }

    private void RemoveTilesBehind(float playerX)
    {
        var removeThreshold = playerX - RemoveBehindDistance;

        while (_activeTiles.Count > 2 && _activeTiles[0].GetTileRightX() < removeThreshold)
        {
            var oldTile = _activeTiles[0];
            _activeTiles.RemoveAt(0);
            oldTile.QueueFree();
        }
    }

    private PackedScene PickStartTile()
    {
        return _groundTiles[(int)(_rng.Randi() % (uint)_groundTiles.Count)];
    }

    private PackedScene PickEndCapTile()
    {
        var currentConnector = _activeTiles.Count > 0
            ? _activeTiles[^1].GetRightConnector()
            : new LevelTileConnector { GroundY = GroundY };

        var pool = GetTilesForConnector(currentConnector);
        return pool.Count > 0 ? pool[(int)(_rng.Randi() % (uint)pool.Count)] : _groundTiles[0];
    }

    private PackedScene PickNextTile()
    {
        var currentConnector = _activeTiles.Count > 0
            ? _activeTiles[^1].GetRightConnector()
            : new LevelTileConnector { GroundY = GroundY };

        var candidates = GetTilesForConnector(currentConnector);
        if (candidates.Count == 0)
            return null;

        var flatWeight = 0.08f;
        var halfPipeWeight = 0.12f;
        var gapWeight = 0.05f;
        var multiWeight = 0.10f;
        var rampWeight = 0.24f;
        var stairWeight = 0.21f;
        var dropWeight = 0.10f;
        var highFlatWeight = 0.10f;

        var totalWeight = 0.0f;
        var weights = new float[candidates.Count];

        for (var i = 0; i < candidates.Count; i++)
        {
            var scenePath = candidates[i].ResourcePath;
            var weight = 1.0f;

            if (scenePath.Contains("FlatRun"))
                weight = flatWeight;
            else if (scenePath.Contains("HalfPipe"))
                weight = halfPipeWeight;
            else if (scenePath.Contains("GapJump"))
                weight = gapWeight;
            else if (scenePath.Contains("MultiLevel"))
                weight = multiWeight;
            else if (scenePath.Contains("RampSection"))
                weight = rampWeight;
            else if (scenePath.Contains("StairClimb"))
                weight = stairWeight;
            else if (scenePath.Contains("DropSection"))
                weight = dropWeight;
            else if (scenePath.Contains("HighFlat"))
                weight = highFlatWeight;

            totalWeight += weight;
            weights[i] = weight;
        }

        var roll = _rng.Randf() * totalWeight;
        for (var i = 0; i < candidates.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0.0f)
                return candidates[i];
        }

        return candidates[^1];
    }

    private List<PackedScene> GetTilesForConnector(LevelTileConnector connector)
    {
        if (Mathf.Abs(connector.GroundY - HighY) <= 0.01f)
            return _descendingTiles.Concat(_highTiles).ToList();

        return _groundTiles.Concat(_risingTiles).ToList();
    }

    private void LoadTilePool()
    {
        _groundTiles.Add(GD.Load<PackedScene>(FlatRunPath));
        _groundTiles.Add(GD.Load<PackedScene>(HalfPipePath));
        _groundTiles.Add(GD.Load<PackedScene>(GapJumpPath));
        _groundTiles.Add(GD.Load<PackedScene>(MultiLevelPath));

        _risingTiles.Add(GD.Load<PackedScene>(RampSectionPath));
        _risingTiles.Add(GD.Load<PackedScene>(StairClimbPath));

        _descendingTiles.Add(GD.Load<PackedScene>(DropSectionPath));

        _highTiles.Add(GD.Load<PackedScene>(HighFlatPath));
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
