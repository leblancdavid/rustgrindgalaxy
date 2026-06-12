using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class LevelSurface01 : MissionLevel
{
    private const string RaiderScenePath = "res://scenes/enemies/Raider.tscn";
    private const string DroneScenePath = "res://scenes/enemies/Drone.tscn";
    private const string PickupScenePath = "res://scenes/world/MineralPickup.tscn";
    private const string ShockHazardScenePath = "res://scenes/world/ShockHazard.tscn";

    private ColorRect _sky = null!;
    private ColorRect _horizon = null!;
    private ColorRect _dustBand = null!;
    private GrindRail _railA = null!;
    private GrindRail _railB = null!;
    private PackedScene _raiderScene = null!;
    private PackedScene _droneScene = null!;
    private PackedScene _pickupScene = null!;
    private PackedScene _shockHazardScene = null!;
    private Node2D _spawnedActors = null!;
    private readonly List<Marker2D> _raiderSpawnMarkers = new();
    private readonly List<Marker2D> _droneSpawnMarkers = new();
    private readonly List<Marker2D> _pickupSpawnMarkers = new();
    private readonly List<Marker2D> _hazardSpawnMarkers = new();
    private LevelColorPalette _palette;

    public override void _Ready()
    {
        _sky = GetNode<ColorRect>("Sky");
        _horizon = GetNode<ColorRect>("Horizon");
        _dustBand = GetNode<ColorRect>("DustBand");
        _railA = GetNode<GrindRail>("RailA");
        _railB = GetNode<GrindRail>("RailB");
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

    public override ExtractionZone GetExtractionZone()
    {
        return GetNode<ExtractionZone>("ExtractionZone");
    }

    public override void ApplyMission(MissionRunData mission)
    {
        _palette = LevelColorPalette.FromMinerals(mission.PrimaryMineral, mission.SecondaryMineral);
        ApplyPalette(_palette);
        ApplyModifiers(mission);
        SpawnMissionContent(mission);
    }

    private void ApplyPalette(LevelColorPalette palette)
    {
        _sky.Color = palette.PrimaryDark;
        _sky.Color = new Color(_sky.Color.R, _sky.Color.G, _sky.Color.B, 1.0f);
        _horizon.Color = palette.PrimaryMedium;
        _dustBand.Color = new Color(palette.SecondaryLight.R, palette.SecondaryLight.G, palette.SecondaryLight.B, 0.18f);
    }

    private void ApplyModifiers(MissionRunData mission)
    {
        var lowVisibility = mission.Modifiers.Contains(MissionModifierType.LowVisibility);
        var signalInterference = mission.Modifiers.Contains(MissionModifierType.SignalInterference);
        var unstableRails = mission.Modifiers.Contains(MissionModifierType.UnstableRails);

        _sky.Modulate = lowVisibility ? new Color(0.72f, 0.72f, 0.78f, 1.0f) : Colors.White;
        _dustBand.Visible = !signalInterference;
        _railA.BaseSpeed = unstableRails ? 180.0f : 148.0f;
        _railB.BaseSpeed = unstableRails ? 194.0f : 160.0f;
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
        SpawnLevelProps(rng);
    }

    private void SpawnEnemies(RandomNumberGenerator rng, float enemyDensity)
    {
        var raiderCount = Mathf.Clamp(Mathf.RoundToInt(_raiderSpawnMarkers.Count * Mathf.Max(0.65f, enemyDensity)), 1, _raiderSpawnMarkers.Count);
        var droneCount = Mathf.Clamp(Mathf.RoundToInt(_droneSpawnMarkers.Count * Mathf.Max(0.3f, enemyDensity - 0.1f)), 0, _droneSpawnMarkers.Count);

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
        var chosenMarkers = PickMarkers(_pickupSpawnMarkers, pickupCount, rng);

        for (var index = 0; index < chosenMarkers.Count; index += 1)
        {
            var pickup = _pickupScene.Instantiate<MineralPickup>();
            _spawnedActors.AddChild(pickup);
            pickup.GlobalPosition = chosenMarkers[index].GlobalPosition;
            pickup.SetMineral(index % 2 == 0 ? primaryMineral : secondaryMineral);
            if (index == 0)
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

    public override void SpawnLevelProps(RandomNumberGenerator rng)
    {
        var palette = PropPalettes.Surface;
        var segments = new[]
        {
            new FloorSegment(0, 320, 156, 156),
            new FloorSegment(49, 107, 129, 129),
            new FloorSegment(144, 196, 103, 103),
            new FloorSegment(231, 277, 79, 79),
        };

        foreach (var seg in segments)
        {
            var count = rng.RandiRange(1, 3);
            for (var i = 0; i < count; i++)
            {
                var localX = rng.RandfRange(seg.StartX + 8, seg.EndX - 8);
                if (seg.EndX - seg.StartX <= 16)
                    continue;

                var template = PickPropTemplate(palette, rng);
                var prop = new Prop();
                prop.Layer = template.Layer;
                prop.Initialize(template.Width, template.Height, template.Color, template.IsLighting, template.Layer, template.Slot, template.GlowYOffset, template.GlowScaleX, template.GlowScaleY);
                prop.ApplyPalette(_palette);
                var baseSink = 5f;
                var fgExtra = template.Layer == Prop.PropLayer.Foreground ? 9f : 0f;
                var groundOffset = baseSink + fgExtra;
                prop.Position = new Vector2(localX, seg.StartY - template.Height / 2f + groundOffset);
                _spawnedActors.AddChild(prop);
            }
        }
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

    private static List<Marker2D> PickMarkers(List<Marker2D> markers, int count, RandomNumberGenerator rng)
    {
        var shuffled = markers.OrderBy(_ => rng.Randi()).ToList();
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
