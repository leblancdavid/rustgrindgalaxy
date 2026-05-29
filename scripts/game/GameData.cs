using System.Collections.Generic;

public sealed class GameData
{
    public List<DiscoveryRecord> Discoveries { get; set; } = new();

    public Dictionary<string, int> RecoveredMinerals { get; set; } = new();

    public int CompletedMissionCount { get; set; }

    public int FailedMissionCount { get; set; }

    public int ProbeLaunchCount { get; set; }
}
