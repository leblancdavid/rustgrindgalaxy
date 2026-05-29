using Godot;

public sealed class DiscoveryGenerator
{
    private readonly RandomNumberGenerator _rng = new();

    private static readonly string[] Prefixes =
    {
        "Ashfall",
        "Kestrel",
        "Orphan",
        "Frostwake",
        "Cinder",
        "Drift",
        "Halo",
        "Rustline",
    };

    private static readonly string[] PlanetSuffixes =
    {
        "Moon",
        "World",
        "Reach",
        "Belt",
        "Frontier",
    };

    private static readonly string[] ShipSuffixes =
    {
        "Cargo Hull",
        "Freighter",
        "Salvage Ark",
        "Carrier",
        "Driftship",
    };

    private static readonly string[] StationSuffixes =
    {
        "Rig",
        "Station",
        "Array",
        "Spindle",
        "Platform",
    };

    public DiscoveryGenerator()
    {
        _rng.Randomize();
    }

    public DiscoveryRecord GenerateDiscovery(ProbeTier probeTier)
    {
        var seed = NextSeed();
        var seededRng = new RandomNumberGenerator { Seed = (ulong)seed };
        var destinationType = RollDestinationType(seededRng);
        var theme = RollEnvironmentTheme(seededRng, probeTier, destinationType);
        var primaryMineral = GetPrimaryMineral(theme);
        var secondaryMineral = RollSecondaryMineral(seededRng, primaryMineral);
        var difficultyTier = RollDifficultyTier(seededRng, probeTier);

        return new DiscoveryRecord
        {
            Id = System.Guid.NewGuid().ToString("N"),
            Seed = seed,
            CreatedAtUnix = (long)Time.GetUnixTimeFromSystem(),
            DisplayName = BuildDisplayName(seededRng, destinationType),
            Description = BuildDescription(destinationType, theme, difficultyTier, primaryMineral),
            ProbeTier = probeTier,
            DestinationType = destinationType,
            EnvironmentTheme = theme,
            DifficultyTier = difficultyTier,
            PrimaryMineral = primaryMineral,
            SecondaryMineral = secondaryMineral,
            TimesVisited = 0,
            IsUnlocked = true,
        };
    }

    public MissionRunData CreateMissionRun(DiscoveryRecord discovery)
    {
        var runSeed = NextSeed() ^ discovery.Seed ^ discovery.TimesVisited;
        var seededRng = new RandomNumberGenerator { Seed = (ulong)runSeed };
        var gravityScale = GetGravityScale(discovery.EnvironmentTheme, seededRng);
        var difficultyFactor = discovery.DifficultyTier / 5.0f;

        return new MissionRunData
        {
            DiscoveryId = discovery.Id,
            RunSeed = runSeed,
            MissionTitle = discovery.DisplayName,
            ThemeLabel = GetThemeDisplayName(discovery.EnvironmentTheme),
            PaletteKey = discovery.EnvironmentTheme.ToString().ToLowerInvariant(),
            GravityScale = gravityScale,
            EnemyDensity = 0.8f + (difficultyFactor * 0.6f) + seededRng.RandfRange(-0.05f, 0.08f),
            PickupDensity = 0.85f + ((6 - discovery.DifficultyTier) * 0.06f) + seededRng.RandfRange(-0.04f, 0.08f),
            MaterialTarget = 3 + discovery.DifficultyTier,
            PrimaryMineral = discovery.PrimaryMineral,
            SecondaryMineral = discovery.SecondaryMineral,
            DifficultyTier = discovery.DifficultyTier,
            LevelTemplateId = "industrial_01",
        };
    }

    public static string GetThemeDisplayName(EnvironmentTheme theme)
    {
        return theme switch
        {
            EnvironmentTheme.Industrial => "Industrial",
            EnvironmentTheme.Rocky => "Rocky",
            EnvironmentTheme.Frozen => "Frozen",
            EnvironmentTheme.Derelict => "Derelict",
            _ => "Unknown",
        };
    }

    private long NextSeed()
    {
        return ((long)_rng.Randi() << 32) | _rng.Randi();
    }

    private static DestinationType RollDestinationType(RandomNumberGenerator rng)
    {
        return rng.RandiRange(0, 2) switch
        {
            0 => DestinationType.Planet,
            1 => DestinationType.AbandonedShip,
            _ => DestinationType.AbandonedStation,
        };
    }

    private static EnvironmentTheme RollEnvironmentTheme(RandomNumberGenerator rng, ProbeTier probeTier, DestinationType destinationType)
    {
        if (destinationType == DestinationType.AbandonedShip)
        {
            return rng.Randf() < 0.65f ? EnvironmentTheme.Derelict : EnvironmentTheme.Industrial;
        }

        if (destinationType == DestinationType.AbandonedStation)
        {
            return rng.Randf() < 0.6f ? EnvironmentTheme.Industrial : EnvironmentTheme.Derelict;
        }

        return probeTier switch
        {
            ProbeTier.Basic => rng.Randf() < 0.65f ? EnvironmentTheme.Rocky : EnvironmentTheme.Industrial,
            ProbeTier.Survey => rng.Randf() < 0.35f ? EnvironmentTheme.Frozen : EnvironmentTheme.Rocky,
            ProbeTier.DeepScan => rng.Randf() < 0.45f ? EnvironmentTheme.Frozen : EnvironmentTheme.Derelict,
            _ => EnvironmentTheme.Industrial,
        };
    }

    private static int RollDifficultyTier(RandomNumberGenerator rng, ProbeTier probeTier)
    {
        return probeTier switch
        {
            ProbeTier.Basic => rng.RandiRange(1, 3),
            ProbeTier.Survey => rng.RandiRange(2, 4),
            ProbeTier.DeepScan => rng.RandiRange(3, 5),
            _ => 1,
        };
    }

    private static MineralType GetPrimaryMineral(EnvironmentTheme theme)
    {
        return theme switch
        {
            EnvironmentTheme.Industrial => MineralType.Cinder,
            EnvironmentTheme.Rocky => MineralType.Solar,
            EnvironmentTheme.Frozen => MineralType.Azure,
            EnvironmentTheme.Derelict => MineralType.Umbra,
            _ => MineralType.Cinder,
        };
    }

    private static MineralType RollSecondaryMineral(RandomNumberGenerator rng, MineralType primary)
    {
        MineralType mineral;
        do
        {
            mineral = (MineralType)rng.RandiRange(0, 5);
        }
        while (mineral == primary);

        return mineral;
    }

    private static string BuildDisplayName(RandomNumberGenerator rng, DestinationType destinationType)
    {
        var prefix = Prefixes[rng.RandiRange(0, Prefixes.Length - 1)];
        var suffix = destinationType switch
        {
            DestinationType.Planet => PlanetSuffixes[rng.RandiRange(0, PlanetSuffixes.Length - 1)],
            DestinationType.AbandonedShip => ShipSuffixes[rng.RandiRange(0, ShipSuffixes.Length - 1)],
            _ => StationSuffixes[rng.RandiRange(0, StationSuffixes.Length - 1)],
        };

        return $"{prefix} {suffix}";
    }

    private static string BuildDescription(DestinationType destinationType, EnvironmentTheme theme, int difficultyTier, MineralType primaryMineral)
    {
        var typeLabel = destinationType switch
        {
            DestinationType.Planet => "world",
            DestinationType.AbandonedShip => "derelict hull",
            _ => "abandoned station",
        };

        return $"A {GetThemeDisplayName(theme).ToLowerInvariant()} {typeLabel} with difficulty {difficultyTier} salvage pressure. Primary returns favor {primaryMineral}.";
    }

    private static float GetGravityScale(EnvironmentTheme theme, RandomNumberGenerator rng)
    {
        return theme switch
        {
            EnvironmentTheme.Industrial => rng.RandfRange(0.95f, 1.05f),
            EnvironmentTheme.Rocky => rng.RandfRange(0.88f, 0.98f),
            EnvironmentTheme.Frozen => rng.RandfRange(0.78f, 0.9f),
            EnvironmentTheme.Derelict => rng.RandfRange(0.7f, 0.86f),
            _ => 1.0f,
        };
    }
}
