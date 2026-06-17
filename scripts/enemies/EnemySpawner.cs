using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class EnemySpawner : Node
{
    private TileLevelGenerator _tileGenerator = null!;
    private readonly Dictionary<string, PackedScene> _sceneMap = new();
    private readonly Dictionary<string, List<Marker2D>> _markerCache = new();
    private Node _spawnParent = null!;

    public void Initialize(TileLevelGenerator generator, Node spawnParent)
    {
        _tileGenerator = generator;
        _spawnParent = spawnParent;

        RegisterEnemyType("Raiders", "res://scenes/enemies/Raider.tscn");
        RegisterEnemyType("Drones", "res://scenes/enemies/Drone.tscn");
        RegisterEnemyType("CombatDrones", "res://scenes/enemies/CombatDrone.tscn");
        RegisterEnemyType("Turrets", "res://scenes/enemies/LaserTurret.tscn");
        RegisterEnemyType("BombBots", "res://scenes/enemies/BombBot.tscn");
        RegisterEnemyType("BoomerangRaiders", "res://scenes/enemies/BoomerangRaider.tscn");
        RegisterEnemyType("MineLayers", "res://scenes/enemies/MineLayer.tscn");
        RegisterEnemyType("Grenadiers", "res://scenes/enemies/Grenadier.tscn");
        RegisterEnemyType("ShockDrones", "res://scenes/enemies/ShockDrone.tscn");
        RegisterEnemyType("SuicideDrones", "res://scenes/enemies/SuicideDrone.tscn");
    }

    public void RegisterEnemyType(string markerGroup, string scenePath)
    {
        _sceneMap[markerGroup] = GD.Load<PackedScene>(scenePath);
    }

    public void RefreshMarkers()
    {
        _markerCache.Clear();
        foreach (var kvp in _sceneMap)
        {
            var markers = new List<Marker2D>();
            _tileGenerator.CollectSpawnMarkers("SpawnMarkers/" + kvp.Key, markers);
            _markerCache[kvp.Key] = markers;
        }
    }

    public void ClearSpawned()
    {
        foreach (var child in _spawnParent.GetChildren())
        {
            if (child is Node2D node && !node.IsQueuedForDeletion())
                node.QueueFree();
        }
    }

    public void SpawnAll(RandomNumberGenerator rng, float enemyDensity, int? maxEnemies = null)
    {
        var totalSpawned = 0;

        foreach (var kvp in _markerCache)
        {
            if (!_sceneMap.TryGetValue(kvp.Key, out var scene))
                continue;

            var markers = kvp.Value;
            if (markers.Count == 0)
                continue;

            var density = enemyDensity;
            // Reduce density for rarer types
            if (kvp.Key is "BombBots" or "SuicideDrones")
                density *= 0.4f;
            else if (kvp.Key is "Turrets" or "Grenadiers" or "ShockDrones")
                density *= 0.6f;

            var count = Mathf.Clamp(Mathf.RoundToInt(markers.Count * density), 0, markers.Count);

            if (maxEnemies.HasValue && totalSpawned + count > maxEnemies.Value)
                count = maxEnemies.Value - totalSpawned;

            foreach (var marker in PickMarkers(markers, count, rng))
            {
                var enemy = scene.Instantiate<Node2D>();
                enemy.GlobalPosition = marker.GlobalPosition;
                _spawnParent.AddChild(enemy);
                totalSpawned++;
            }
        }
    }

    private static List<Marker2D> PickMarkers(List<Marker2D> markers, int count, RandomNumberGenerator rng)
    {
        var shuffled = markers
            .OrderBy(_ => rng.Randi())
            .ToList();

        if (count < shuffled.Count)
            shuffled.RemoveRange(count, shuffled.Count - count);

        shuffled.Sort((left, right) => left.GlobalPosition.X.CompareTo(right.GlobalPosition.X));
        return shuffled;
    }
}
