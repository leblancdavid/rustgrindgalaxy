using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class World : Node2D
{
    [Export] public PackedScene? ReturnScene;
    [Export] public float RestartDelaySeconds = 0.75f;
    [Export] public int MissionMaterialTarget = 4;

    public PlayerController Player { get; private set; } = null!;

    public Hud HudLayer { get; private set; } = null!;

    public ExtractionZone ExtractionZone { get; private set; } = null!;

    public PlayerLoadout DebugLoadout { get; private set; } = null!;

    public Dictionary<MineralType, int> CollectedMinerals { get; } = new();

    private GameState _gameState = null!;
    private MissionRunData _mission = null!;
    private float _deathTimer;

    private bool _restartReady;

    private bool _missionComplete;

    public override void _Ready()
    {
        _gameState = GetNode<GameState>("/root/GameState");
        _mission = _gameState.ActiveMission ?? MissionRunData.CreateFallback();
        Player = GetNode<PlayerController>("Player");
        HudLayer = GetNode<Hud>("Hud");
        ExtractionZone = GetNode<ExtractionZone>("Level/ExtractionZone");

        var generator = new ModuleGenerator();
        DebugLoadout = generator.GenerateDebugLoadout(ModuleRarity.Rare);
        Player.SetLoadout(DebugLoadout);
        Player.GravityScale = _mission.GravityScale;
        MissionMaterialTarget = _mission.MaterialTarget;
        ApplyMissionModifiers();

        if (GetNodeOrNull<LevelIndustrial01>("Level") is { } level)
        {
            level.ApplyMission(_mission);
        }

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
                ReturnToTerminal(true);
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
                ReturnToTerminal(false);
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

    public string GetMissionTitle()
    {
        return _mission.MissionTitle;
    }

    public string GetMissionThemeLabel()
    {
        return _mission.ThemeLabel;
    }

    public int GetMissionDifficulty()
    {
        return _mission.DifficultyTier;
    }

    public string GetMissionModifierSummary()
    {
        return _mission.GetModifierSummary();
    }

    private void UpdateExtractionState()
    {
        ExtractionZone?.SetActive(CanExtract());
    }

    private void ApplyMissionModifiers()
    {
        foreach (var modifier in _mission.Modifiers)
        {
            switch (modifier)
            {
                case MissionModifierType.RichVeins:
                    MissionMaterialTarget = Mathf.Max(2, MissionMaterialTarget - 1);
                    break;
                case MissionModifierType.SignalInterference:
                    RestartDelaySeconds = 1.0f;
                    break;
            }
        }
    }

    private void ReturnToTerminal(bool missionSucceeded)
    {
        if (missionSucceeded)
        {
            _gameState.CompleteActiveMission(CollectedMinerals);
        }
        else
        {
            _gameState.FailActiveMission();
        }

        var targetScene = ReturnScene ?? GD.Load<PackedScene>("res://scenes/ui/MissionResults.tscn");
        GetTree().ChangeSceneToPacked(targetScene);
    }
}
