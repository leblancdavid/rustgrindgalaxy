using System.Collections.Generic;

public sealed class EnvironmentProfile
{
    public EnvironmentTheme Theme { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string PaletteKey { get; init; } = string.Empty;

    public MineralType PrimaryMineral { get; init; } = MineralType.Cinder;

    public float GravityMin { get; init; } = 1.0f;

    public float GravityMax { get; init; } = 1.0f;

    public float BaseHazardDensity { get; init; } = 0.5f;

    public string DefaultLevelTemplateId { get; init; } = "industrial_01";

    public List<MissionModifierType> CommonModifiers { get; init; } = new();

    public List<string> AllowedLevelTemplateIds { get; init; } = new();
}
