using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class World : Node2D
{
    [Export] public float RestartDelaySeconds = 0.75f;
    [Export] public int MissionMaterialTarget = 4;

    public PlayerController Player { get; private set; } = null!;

    public Hud HudLayer { get; private set; } = null!;

    public ExtractionZone ExtractionZone { get; private set; } = null!;

    public PlayerLoadout DebugLoadout { get; private set; } = null!;

    public Dictionary<MineralType, int> CollectedMinerals { get; } = new();

    private float _deathTimer;

    private bool _restartReady;

    private bool _missionComplete;

    public override void _Ready()
    {
        Player = GetNode<PlayerController>("Player");
        HudLayer = GetNode<Hud>("Hud");
        ExtractionZone = GetNode<ExtractionZone>("Level/ExtractionZone");

        var generator = new ModuleGenerator();
        DebugLoadout = generator.GenerateDebugLoadout(ModuleRarity.Rare);
        Player.SetLoadout(DebugLoadout);

        foreach (var mineral in System.Enum.GetValues<MineralType>())
        {
            CollectedMinerals[mineral] = 0;
        }

        GD.Print("Generated debug module loadout:");
        foreach (var module in DebugLoadout.GetAllModules())
        {
            GD.Print($" - {module.GetDebugSummary()}");
        }

        UpdateExtractionState();
    }

    public override void _Process(double delta)
    {
        HudLayer.UpdatePlayerState(Player);

        if (_missionComplete)
        {
            if (Input.IsActionJustPressed("ui_accept"))
            {
                GetTree().ReloadCurrentScene();
                return;
            }

            return;
        }

        if (Player.IsDead)
        {
            if (_restartReady == false)
            {
                _deathTimer += (float)delta;
                if (_deathTimer >= RestartDelaySeconds)
                {
                    _restartReady = true;
                }
            }

            if (_restartReady && Input.IsActionJustPressed("ui_accept"))
            {
                GetTree().ReloadCurrentScene();
                return;
            }
        }
        else
        {
            _deathTimer = 0.0f;
            _restartReady = false;
        }
    }

    public bool IsRestartReady()
    {
        return _restartReady;
    }

    public bool IsMissionComplete()
    {
        return _missionComplete;
    }

    public bool CanExtract()
    {
        return GetTotalCollectedMinerals() >= MissionMaterialTarget;
    }

    public void CollectMineral(MineralType mineral, int amount)
    {
        CollectedMinerals[mineral] = CollectedMinerals.GetValueOrDefault(mineral) + amount;
        UpdateExtractionState();
    }

    public int GetTotalCollectedMinerals()
    {
        return CollectedMinerals.Values.Sum();
    }

    public string GetCollectedMineralSummary()
    {
        var collected = CollectedMinerals
            .Where(entry => entry.Value > 0)
            .Select(entry => $"{entry.Key}:{entry.Value}")
            .ToList();

        return collected.Count > 0 ? string.Join(" ", collected) : "None";
    }

    public void TryCompleteMission()
    {
        if (_missionComplete || Player.IsDead || CanExtract() == false)
        {
            return;
        }

        _missionComplete = true;
    }

    private void UpdateExtractionState()
    {
        ExtractionZone?.SetActive(CanExtract());
    }
}
