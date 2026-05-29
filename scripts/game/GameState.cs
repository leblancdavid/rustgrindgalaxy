using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public partial class GameState : Node
{
    private const string SavePath = "user://savegame.json";

    private readonly DiscoveryGenerator _generator = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
    };

    public GameData Data { get; private set; } = new();

    public MissionRunData? ActiveMission { get; private set; }

    public override void _Ready()
    {
        LoadData();
        EnsureRecoveredMineralKeys();
    }

    public IReadOnlyList<DiscoveryRecord> GetDiscoveries()
    {
        return Data.Discoveries
            .OrderByDescending(discovery => discovery.CreatedAtUnix)
            .ToList();
    }

    public DiscoveryRecord GenerateDiscovery(ProbeTier probeTier)
    {
        var discovery = _generator.GenerateDiscovery(probeTier);
        Data.Discoveries.Add(discovery);
        Data.ProbeLaunchCount += 1;
        SaveData();
        return discovery;
    }

    public DiscoveryRecord? GetDiscovery(string discoveryId)
    {
        return Data.Discoveries.FirstOrDefault(discovery => discovery.Id == discoveryId);
    }

    public bool PrepareMission(string discoveryId)
    {
        var discovery = GetDiscovery(discoveryId);
        if (discovery == null)
        {
            return false;
        }

        discovery.TimesVisited += 1;
        ActiveMission = _generator.CreateMissionRun(discovery);
        SaveData();
        return true;
    }

    public void CompleteActiveMission(IReadOnlyDictionary<MineralType, int> collectedMinerals)
    {
        if (ActiveMission == null)
        {
            return;
        }

        foreach (var entry in collectedMinerals)
        {
            var key = entry.Key.ToString();
            Data.RecoveredMinerals[key] = Data.RecoveredMinerals.GetValueOrDefault(key) + entry.Value;
        }

        Data.CompletedMissionCount += 1;
        ActiveMission = null;
        SaveData();
    }

    public void FailActiveMission()
    {
        if (ActiveMission == null)
        {
            return;
        }

        Data.FailedMissionCount += 1;
        ActiveMission = null;
        SaveData();
    }

    public int GetTotalRecoveredMinerals()
    {
        EnsureRecoveredMineralKeys();
        return Data.RecoveredMinerals.Values.Sum();
    }

    public string GetRecoveredMineralSummary()
    {
        EnsureRecoveredMineralKeys();

        var parts = Data.RecoveredMinerals
            .Where(entry => entry.Value > 0)
            .Select(entry => $"{entry.Key}:{entry.Value}")
            .ToList();

        return parts.Count > 0 ? string.Join(" ", parts) : "None";
    }

    private void LoadData()
    {
        if (FileAccess.FileExists(SavePath) == false)
        {
            Data = new GameData();
            SaveData();
            return;
        }

        try
        {
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            var json = file.GetAsText();
            Data = JsonSerializer.Deserialize<GameData>(json, _jsonOptions) ?? new GameData();
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Failed to load save data: {exception.Message}");
            Data = new GameData();
        }
    }

    private void SaveData()
    {
        EnsureRecoveredMineralKeys();

        try
        {
            var json = JsonSerializer.Serialize(Data, _jsonOptions);
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            file.StoreString(json);
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Failed to save data: {exception.Message}");
        }
    }

    private void EnsureRecoveredMineralKeys()
    {
        foreach (var mineral in Enum.GetValues<MineralType>())
        {
            var key = mineral.ToString();
            if (Data.RecoveredMinerals.ContainsKey(key) == false)
            {
                Data.RecoveredMinerals[key] = 0;
            }
        }
    }
}
