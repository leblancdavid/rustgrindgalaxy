using Godot;
using System.Collections.Generic;

public partial class LevelIndustrial01 : Node2D
{
    private ColorRect _backdrop = null!;
    private ColorRect _upperWall = null!;
    private ColorRect _midStripe = null!;
    private readonly List<RaiderEnemy> _raiders = new();
    private readonly List<DroneEnemy> _drones = new();
    private readonly List<MineralPickup> _pickups = new();

    public override void _Ready()
    {
        _backdrop = GetNode<ColorRect>("Backdrop");
        _upperWall = GetNode<ColorRect>("UpperWall");
        _midStripe = GetNode<ColorRect>("MidStripe");

        AddIfPresent(_raiders, "RaiderA");
        AddIfPresent(_drones, "DroneA");
        AddIfPresent(_pickups, "PickupA");
        AddIfPresent(_pickups, "PickupB");
        AddIfPresent(_pickups, "PickupC");
        AddIfPresent(_pickups, "PickupD");
    }

    public void ApplyMission(MissionRunData mission)
    {
        ApplyPalette(mission.PaletteKey);
        ApplyEnemyDensity(mission.EnemyDensity);
        ApplyPickupDensity(mission.PickupDensity, mission.PrimaryMineral, mission.SecondaryMineral);
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

    private void ApplyEnemyDensity(float enemyDensity)
    {
        var enableDrone = enemyDensity >= 0.95f;
        var enableRaider = enemyDensity >= 0.7f;

        foreach (var raider in _raiders)
        {
            raider.Visible = enableRaider;
            raider.ProcessMode = enableRaider ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
            if (raider.HasNode("CollisionShape2D"))
            {
                raider.GetNode<CollisionShape2D>("CollisionShape2D").Disabled = !enableRaider;
            }

            if (raider.HasNode("HurtArea/CollisionShape2D"))
            {
                raider.GetNode<CollisionShape2D>("HurtArea/CollisionShape2D").Disabled = !enableRaider;
            }
        }

        foreach (var drone in _drones)
        {
            drone.Visible = enableDrone;
            drone.ProcessMode = enableDrone ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
            if (drone.HasNode("CollisionShape2D"))
            {
                drone.GetNode<CollisionShape2D>("CollisionShape2D").Disabled = !enableDrone;
            }
        }
    }

    private void ApplyPickupDensity(float pickupDensity, MineralType primaryMineral, MineralType secondaryMineral)
    {
        for (var index = 0; index < _pickups.Count; index += 1)
        {
            var pickup = _pickups[index];
            var enabled = index < Mathf.Clamp(Mathf.RoundToInt(_pickups.Count * pickupDensity), 1, _pickups.Count);
            pickup.Visible = enabled;
            pickup.Monitoring = enabled;
            pickup.Monitorable = enabled;
            pickup.GetNode<CollisionShape2D>("CollisionShape2D").Disabled = !enabled;

            pickup.SetMineral(index % 2 == 0 ? primaryMineral : secondaryMineral);
        }
    }

    private void AddIfPresent<T>(List<T> list, string nodeName) where T : Node
    {
        var node = GetNodeOrNull<T>(nodeName);
        if (node != null)
        {
            list.Add(node);
        }
    }
}
