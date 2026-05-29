using System.Collections.Generic;

public sealed class MissionRunData
{
    public string DiscoveryId { get; set; } = string.Empty;

    public long RunSeed { get; set; }

    public string MissionTitle { get; set; } = "Industrial Test Run";

    public string ThemeLabel { get; set; } = "Industrial";

    public string PaletteKey { get; set; } = "industrial";

    public float GravityScale { get; set; } = 1.0f;

    public float EnemyDensity { get; set; } = 1.0f;

    public float PickupDensity { get; set; } = 1.0f;

    public float HazardDensity { get; set; } = 0.5f;

    public int MaterialTarget { get; set; } = 4;

    public MineralType PrimaryMineral { get; set; } = MineralType.Cinder;

    public MineralType SecondaryMineral { get; set; } = MineralType.Azure;

    public int DifficultyTier { get; set; } = 1;

    public string LevelTemplateId { get; set; } = "industrial_01";

    public List<MissionModifierType> Modifiers { get; set; } = new();

    public string GetModifierSummary()
    {
        return Modifiers.Count > 0 ? string.Join(", ", Modifiers) : "None";
    }

    public static MissionRunData CreateFallback()
    {
        return new MissionRunData();
    }
}
