public sealed class DiscoveryRecord
{
    public string Id { get; set; } = string.Empty;

    public long Seed { get; set; }

    public long CreatedAtUnix { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ProbeTier ProbeTier { get; set; }

    public DestinationType DestinationType { get; set; }

    public EnvironmentTheme EnvironmentTheme { get; set; }

    public int DifficultyTier { get; set; } = 1;

    public MineralType PrimaryMineral { get; set; } = MineralType.Cinder;

    public MineralType SecondaryMineral { get; set; } = MineralType.Azure;

    public int TimesVisited { get; set; }

    public bool IsUnlocked { get; set; } = true;
}
