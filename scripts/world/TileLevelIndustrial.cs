using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class TileLevelIndustrial : MissionLevel
{
    private const string RaiderScenePath = "res://scenes/enemies/Raider.tscn";
    private const string DroneScenePath = "res://scenes/enemies/Drone.tscn";
    private const string PickupScenePath = "res://scenes/world/MineralPickup.tscn";
    private const string ShockHazardScenePath = "res://scenes/world/ShockHazard.tscn";

    private ColorRect _backdrop = null!;
    private ColorRect _upperWall = null!;
    private ColorRect _midStripe = null!;
    private TileLevelGenerator _tileGenerator = null!;
    private Sprite2D _mistFog = null!;

    public TileLevelGenerator TileGenerator => _tileGenerator;
    private Node2D _spawnedActors = null!;
    private PackedScene _raiderScene = null!;
    private PackedScene _droneScene = null!;
    private PackedScene _pickupScene = null!;
    private PackedScene _shockHazardScene = null!;
    private readonly List<GrindRail> _rails = new();
    private readonly List<Marker2D> _raiderSpawnMarkers = new();
    private readonly List<Marker2D> _droneSpawnMarkers = new();
    private readonly List<Marker2D> _pickupSpawnMarkers = new();
    private readonly List<Marker2D> _hazardSpawnMarkers = new();

    public override void _Ready()
    {
        _backdrop = GetNode<ColorRect>("Backdrop");
        _upperWall = GetNode<ColorRect>("UpperWall");
        _midStripe = GetNode<ColorRect>("MidStripe");
        _tileGenerator = GetNode<TileLevelGenerator>("TileGenerator");
        _spawnedActors = GetNode<Node2D>("SpawnedActors");
        _raiderScene = GD.Load<PackedScene>(RaiderScenePath);
        _droneScene = GD.Load<PackedScene>(DroneScenePath);
        _pickupScene = GD.Load<PackedScene>(PickupScenePath);
        _shockHazardScene = GD.Load<PackedScene>(ShockHazardScenePath);
    }

    public override void _Process(double delta)
    {
        _tileGenerator.UpdateStreaming();
        RefreshTileCaches();

        var player = GetTree().Root.GetNodeOrNull<PlayerController>("World/Player");
        if (player != null)
        {
            var camera = player.GetNodeOrNull<Camera2D>("Camera2D");
            if (camera != null)
                _mistFog.Position = new Vector2(
                    camera.GlobalPosition.X - 1500,
                    20);
        }
    }

    public override ExtractionZone GetExtractionZone()
    {
        return GetNode<ExtractionZone>("ExtractionZone");
    }

    public override void ApplyMission(MissionRunData mission)
    {
        var palette = LevelColorPalette.FromMinerals(mission.PrimaryMineral, mission.SecondaryMineral);

        var rng = new RandomNumberGenerator { Seed = (ulong)(mission.RunSeed ^ 0x51F15EED) };
        var darkenBg = rng.Randf() < 0.5f;
        var bgDim = darkenBg ? 0.65f : 1.0f;
        var fgPalette = darkenBg ? palette : palette.WithBrightness(0.85f);

        ApplyPalette(palette, bgDim);

        var player = GetTree().Root.GetNodeOrNull<PlayerController>("World/Player");
        _tileGenerator.Initialize(player!, this, mission.RunSeed ^ 0x51F15EED, fgPalette);
        _tileGenerator.SetExtractionZone(GetExtractionZone());
        _tileGenerator.BuildInitial();

        RefreshTileCaches();
        ApplyModifiers(mission);
        SpawnMissionContent(mission);
    }

    private void RefreshTileCaches()
    {
        _tileGenerator.CollectRails(_rails);
        _tileGenerator.CollectSpawnMarkers("SpawnMarkers/Raiders", _raiderSpawnMarkers);
        _tileGenerator.CollectSpawnMarkers("SpawnMarkers/Drones", _droneSpawnMarkers);
        _tileGenerator.CollectSpawnMarkers("SpawnMarkers/PickupMarkers", _pickupSpawnMarkers);
        _tileGenerator.CollectSpawnMarkers("SpawnMarkers/Hazards", _hazardSpawnMarkers);
    }

    private void ApplyPalette(LevelColorPalette palette, float bgDim = 1.0f)
    {
        _backdrop.Color = palette.PrimaryDark * bgDim;
        _backdrop.Color = new Color(_backdrop.Color.R, _backdrop.Color.G, _backdrop.Color.B, 1.0f);
        _upperWall.Color = new Color(palette.PrimaryMedium.R * bgDim, palette.PrimaryMedium.G * bgDim, palette.PrimaryMedium.B * bgDim, 1f);
        _midStripe.Color = new Color(palette.SecondaryLight.R * bgDim, palette.SecondaryLight.G * bgDim, palette.SecondaryLight.B * bgDim, 0.35f);

        _mistFog = MistFog.CreateMist(palette, bgDim, 400);
        AddChild(_mistFog);
        MoveChild(_mistFog, 3);
        _mistFog.Position = new Vector2(-1500, 20);
    }

    private void ApplyModifiers(MissionRunData mission)
    {
        var lowVisibility = mission.Modifiers.Contains(MissionModifierType.LowVisibility);
        var signalInterference = mission.Modifiers.Contains(MissionModifierType.SignalInterference);
        var unstableRails = mission.Modifiers.Contains(MissionModifierType.UnstableRails);

        _backdrop.Modulate = lowVisibility ? new Color(0.65f, 0.65f, 0.7f, 1.0f) : Colors.White;
        _midStripe.Visible = !signalInterference;

        for (var index = 0; index < _rails.Count; index += 1)
        {
            var baseSpeed = 150.0f + (index * 12.0f);
            _rails[index].BaseSpeed = unstableRails ? baseSpeed + 28.0f : baseSpeed;
        }
    }

    private void SpawnMissionContent(MissionRunData mission)
    {
        foreach (var child in _spawnedActors.GetChildren())
        {
            child.QueueFree();
        }

        RefreshTileCaches();
        var rng = new RandomNumberGenerator { Seed = (ulong)mission.RunSeed };
        SpawnEnemies(rng, mission.EnemyDensity);
        SpawnPickups(rng, mission.PickupDensity, mission.PrimaryMineral, mission.SecondaryMineral);
        SpawnHazards(rng, mission.HazardDensity, mission.PaletteKey);
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
}
