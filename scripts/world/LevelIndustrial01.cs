using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class LevelIndustrial01 : Node2D
{
    private const string RaiderScenePath = "res://scenes/enemies/Raider.tscn";
    private const string DroneScenePath = "res://scenes/enemies/Drone.tscn";
    private const string PickupScenePath = "res://scenes/world/MineralPickup.tscn";
    private const string ShockHazardScenePath = "res://scenes/world/ShockHazard.tscn";

    private ColorRect _backdrop = null!;
    private ColorRect _upperWall = null!;
    private ColorRect _midStripe = null!;
    private Node2D _railA = null!;
    private Node2D _railB = null!;
    private PackedScene _raiderScene = null!;
    private PackedScene _droneScene = null!;
    private PackedScene _pickupScene = null!;
    private PackedScene _shockHazardScene = null!;
    private Node2D _spawnedActors = null!;
    private readonly List<Marker2D> _raiderSpawnMarkers = new();
    private readonly List<Marker2D> _droneSpawnMarkers = new();
    private readonly List<Marker2D> _pickupSpawnMarkers = new();
    private readonly List<Marker2D> _hazardSpawnMarkers = new();

    public override void _Ready()
    {
        _backdrop = GetNode<ColorRect>("Backdrop");
        _upperWall = GetNode<ColorRect>("UpperWall");
        _midStripe = GetNode<ColorRect>("MidStripe");
        _railA = GetNode<Node2D>("RailA");
        _railB = GetNode<Node2D>("RailB");
        _raiderScene = GD.Load<PackedScene>(RaiderScenePath);
        _droneScene = GD.Load<PackedScene>(DroneScenePath);
        _pickupScene = GD.Load<PackedScene>(PickupScenePath);
        _shockHazardScene = GD.Load<PackedScene>(ShockHazardScenePath);
        _spawnedActors = GetNode<Node2D>("SpawnedActors");

        CacheMarkerChildren("SpawnMarkers/Raiders", _raiderSpawnMarkers);
        CacheMarkerChildren("SpawnMarkers/Drones", _droneSpawnMarkers);
        CacheMarkerChildren("SpawnMarkers/Pickups", _pickupSpawnMarkers);
        CacheMarkerChildren("SpawnMarkers/Hazards", _hazardSpawnMarkers);
    }

    public void ApplyMission(MissionRunData mission)
    {
        ApplyPalette(mission.PaletteKey);
        ApplyModifiers(mission);
        SpawnMissionContent(mission);
    }

    private void ApplyPalette(string paletteKey)
    {
        switch (paletteKey)
        {
            case "rocky":
                _backdrop.Color = new Color(0.121f, 0.094f, 0.09f, 1.0f);
                _upperWall.Color = new Color(0.266f, 0.2f, 0.153f, 1.0f);
                _midStripe.Color = new Color(0.62f, 0.403f, 0.215f, 0.35f);
                break;
            case "frozen":
                _backdrop.Color = new Color(0.05f, 0.08f, 0.14f, 1.0f);
                _upperWall.Color = new Color(0.13f, 0.2f, 0.32f, 1.0f);
                _midStripe.Color = new Color(0.49f, 0.77f, 0.94f, 0.3f);
                break;
            case "derelict":
                _backdrop.Color = new Color(0.07f, 0.055f, 0.09f, 1.0f);
                _upperWall.Color = new Color(0.18f, 0.13f, 0.2f, 1.0f);
                _midStripe.Color = new Color(0.59f, 0.33f, 0.67f, 0.28f);
                break;
            default:
                _backdrop.Color = new Color(0.0784314f, 0.0980392f, 0.121569f, 1.0f);
                _upperWall.Color = new Color(0.12549f, 0.152941f, 0.188235f, 1.0f);
                _midStripe.Color = new Color(0.227451f, 0.447059f, 0.529412f, 0.4f);
                break;
        }
    }

    private void SpawnMissionContent(MissionRunData mission)
    {
        foreach (var child in _spawnedActors.GetChildren())
        {
            child.QueueFree();
        }

        var rng = new RandomNumberGenerator { Seed = (ulong)mission.RunSeed };
        SpawnEnemies(rng, mission.EnemyDensity);
        SpawnPickups(rng, mission.PickupDensity, mission.PrimaryMineral, mission.SecondaryMineral);
        SpawnHazards(rng, mission.HazardDensity, mission.PaletteKey);
    }

    private void ApplyModifiers(MissionRunData mission)
    {
        var lowVisibility = mission.Modifiers.Contains(MissionModifierType.LowVisibility);
        var signalInterference = mission.Modifiers.Contains(MissionModifierType.SignalInterference);
        var unstableRails = mission.Modifiers.Contains(MissionModifierType.UnstableRails);

        _backdrop.Modulate = lowVisibility ? new Color(0.65f, 0.65f, 0.7f, 1.0f) : Colors.White;
        _midStripe.Visible = !signalInterference;

        if (_railA is GrindRail railA)
        {
            railA.BaseSpeed = unstableRails ? 188.0f : 150.0f;
        }

        if (_railB is GrindRail railB)
        {
            railB.BaseSpeed = unstableRails ? 210.0f : 172.0f;
        }
    }

    private void SpawnEnemies(RandomNumberGenerator rng, float enemyDensity)
    {
        var raiderCount = Mathf.Clamp(Mathf.RoundToInt(_raiderSpawnMarkers.Count * enemyDensity), 1, _raiderSpawnMarkers.Count);
        var droneCount = Mathf.Clamp(Mathf.RoundToInt(_droneSpawnMarkers.Count * Mathf.Max(0.35f, enemyDensity - 0.15f)), 0, _droneSpawnMarkers.Count);

        foreach (var marker in PickMarkers(_raiderSpawnMarkers, raiderCount, rng))
        {
            var raider = _raiderScene.Instantiate<RaiderEnemy>();
            _spawnedActors.AddChild(raider);
            raider.GlobalPosition = marker.GlobalPosition;
        }

        foreach (var marker in PickMarkers(_droneSpawnMarkers, droneCount, rng))
        {
            var drone = _droneScene.Instantiate<DroneEnemy>();
            _spawnedActors.AddChild(drone);
            drone.GlobalPosition = marker.GlobalPosition;
        }
    }

    private void SpawnPickups(RandomNumberGenerator rng, float pickupDensity, MineralType primaryMineral, MineralType secondaryMineral)
    {
        var pickupCount = Mathf.Clamp(Mathf.RoundToInt(_pickupSpawnMarkers.Count * pickupDensity), 1, _pickupSpawnMarkers.Count);
        var minerals = new[]
        {
            primaryMineral,
            secondaryMineral,
            primaryMineral,
            secondaryMineral,
            primaryMineral,
        };

        var chosenMarkers = PickMarkers(_pickupSpawnMarkers, pickupCount, rng);
        for (var index = 0; index < chosenMarkers.Count; index += 1)
        {
            var pickup = _pickupScene.Instantiate<MineralPickup>();
            _spawnedActors.AddChild(pickup);
            pickup.GlobalPosition = chosenMarkers[index].GlobalPosition;
            pickup.SetMineral(minerals[index % minerals.Length]);
            if (index == chosenMarkers.Count - 1 && chosenMarkers.Count > 2)
            {
                pickup.Amount = 2;
            }
        }
    }

    private void SpawnHazards(RandomNumberGenerator rng, float hazardDensity, string paletteKey)
    {
        var hazardCount = Mathf.Clamp(Mathf.RoundToInt(_hazardSpawnMarkers.Count * hazardDensity), 0, _hazardSpawnMarkers.Count);
        foreach (var marker in PickMarkers(_hazardSpawnMarkers, hazardCount, rng))
        {
            var hazard = _shockHazardScene.Instantiate<ShockHazard>();
            _spawnedActors.AddChild(hazard);
            hazard.GlobalPosition = marker.GlobalPosition;
            hazard.SetTheme(paletteKey);
        }
    }

    private static List<Marker2D> PickMarkers(List<Marker2D> markers, int count, RandomNumberGenerator rng)
    {
        var shuffled = markers
            .OrderBy(_ => rng.Randi())
            .ToList();

        if (count < shuffled.Count)
        {
            shuffled.RemoveRange(count, shuffled.Count - count);
        }

        shuffled.Sort((left, right) => left.GlobalPosition.X.CompareTo(right.GlobalPosition.X));
        return shuffled;
    }

    private void CacheMarkerChildren(string path, List<Marker2D> target)
    {
        var parent = GetNode<Node>(path);
        foreach (var child in parent.GetChildren())
        {
            if (child is Marker2D marker)
            {
                target.Add(marker);
            }
        }
    }
}
