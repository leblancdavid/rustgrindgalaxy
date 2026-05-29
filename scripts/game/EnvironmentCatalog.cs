using Godot;
using System.Collections.Generic;

public static class EnvironmentCatalog
{
    private static readonly IReadOnlyDictionary<EnvironmentTheme, EnvironmentProfile> Profiles =
        new Dictionary<EnvironmentTheme, EnvironmentProfile>
        {
            [EnvironmentTheme.Industrial] = new EnvironmentProfile
            {
                Theme = EnvironmentTheme.Industrial,
                DisplayName = "Industrial",
                PaletteKey = "industrial",
                PrimaryMineral = MineralType.Cinder,
                GravityMin = 0.95f,
                GravityMax = 1.05f,
                BaseHazardDensity = 0.55f,
                DefaultLevelTemplateId = "industrial_01",
                CommonModifiers = new List<MissionModifierType>
                {
                    MissionModifierType.RichVeins,
                    MissionModifierType.SignalInterference,
                },
                AllowedLevelTemplateIds = new List<string>
                {
                    "industrial_01",
                },
            },
            [EnvironmentTheme.Rocky] = new EnvironmentProfile
            {
                Theme = EnvironmentTheme.Rocky,
                DisplayName = "Rocky",
                PaletteKey = "rocky",
                PrimaryMineral = MineralType.Solar,
                GravityMin = 0.88f,
                GravityMax = 0.98f,
                BaseHazardDensity = 0.45f,
                DefaultLevelTemplateId = "surface_01",
                CommonModifiers = new List<MissionModifierType>
                {
                    MissionModifierType.RichVeins,
                },
                AllowedLevelTemplateIds = new List<string>
                {
                    "surface_01",
                    "industrial_01",
                },
            },
            [EnvironmentTheme.Frozen] = new EnvironmentProfile
            {
                Theme = EnvironmentTheme.Frozen,
                DisplayName = "Frozen",
                PaletteKey = "frozen",
                PrimaryMineral = MineralType.Azure,
                GravityMin = 0.78f,
                GravityMax = 0.9f,
                BaseHazardDensity = 0.65f,
                DefaultLevelTemplateId = "surface_01",
                CommonModifiers = new List<MissionModifierType>
                {
                    MissionModifierType.LowVisibility,
                },
                AllowedLevelTemplateIds = new List<string>
                {
                    "surface_01",
                    "derelict_01",
                },
            },
            [EnvironmentTheme.Derelict] = new EnvironmentProfile
            {
                Theme = EnvironmentTheme.Derelict,
                DisplayName = "Derelict",
                PaletteKey = "derelict",
                PrimaryMineral = MineralType.Umbra,
                GravityMin = 0.7f,
                GravityMax = 0.86f,
                BaseHazardDensity = 0.75f,
                DefaultLevelTemplateId = "derelict_01",
                CommonModifiers = new List<MissionModifierType>
                {
                    MissionModifierType.LowVisibility,
                    MissionModifierType.SignalInterference,
                    MissionModifierType.UnstableRails,
                },
                AllowedLevelTemplateIds = new List<string>
                {
                    "derelict_01",
                    "surface_01",
                },
            },
        };

    public static EnvironmentProfile GetProfile(EnvironmentTheme theme)
    {
        return Profiles[theme];
    }

    public static string GetDisplayName(EnvironmentTheme theme)
    {
        return GetProfile(theme).DisplayName;
    }

    public static float RollGravityScale(EnvironmentTheme theme, RandomNumberGenerator rng)
    {
        var profile = GetProfile(theme);
        return rng.RandfRange(profile.GravityMin, profile.GravityMax);
    }

    public static float RollHazardDensity(EnvironmentTheme theme, float difficultyFactor, RandomNumberGenerator rng)
    {
        var profile = GetProfile(theme);
        return Mathf.Clamp(profile.BaseHazardDensity + (difficultyFactor * 0.35f) + rng.RandfRange(-0.08f, 0.08f), 0.2f, 1.25f);
    }

    public static string GetHazardPressureText(EnvironmentTheme theme, int difficultyTier)
    {
        var score = difficultyTier + (theme is EnvironmentTheme.Derelict or EnvironmentTheme.Frozen ? 1 : 0);
        return score switch
        {
            <= 2 => "Low",
            <= 4 => "Moderate",
            <= 6 => "High",
            _ => "Severe",
        };
    }

    public static string GetLikelyModifierPreview(EnvironmentTheme theme, int difficultyTier)
    {
        var profile = GetProfile(theme);
        var modifiers = new List<string>();

        foreach (var modifier in profile.CommonModifiers)
        {
            if (modifier == MissionModifierType.UnstableRails && difficultyTier < 4)
            {
                continue;
            }

            modifiers.Add(modifier.ToString());
        }

        return modifiers.Count > 0 ? string.Join(", ", modifiers) : "None";
    }
}
