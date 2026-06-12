using Godot;

public enum PaletteSlot
{
    PrimaryDark,
    PrimaryMedium,
    PrimaryLight,
    SecondaryDark,
    SecondaryMedium,
    SecondaryLight,
}

public struct LevelColorPalette
{
    public Color PrimaryDark;
    public Color PrimaryMedium;
    public Color PrimaryLight;
    public Color SecondaryDark;
    public Color SecondaryMedium;
    public Color SecondaryLight;

    public readonly Color Resolve(PaletteSlot slot)
    {
        return slot switch
        {
            PaletteSlot.PrimaryDark => PrimaryDark,
            PaletteSlot.PrimaryMedium => PrimaryMedium,
            PaletteSlot.PrimaryLight => PrimaryLight,
            PaletteSlot.SecondaryDark => SecondaryDark,
            PaletteSlot.SecondaryMedium => SecondaryMedium,
            PaletteSlot.SecondaryLight => SecondaryLight,
            _ => Colors.White,
        };
    }

    private static readonly Color CinderLight = Color.FromHtml("#F07830");
    private static readonly Color CinderMedium = Color.FromHtml("#C04718");
    private static readonly Color CinderDark = Color.FromHtml("#802808");

    private static readonly Color VerdantLight = Color.FromHtml("#60D060");
    private static readonly Color VerdantMedium = Color.FromHtml("#38A828");
    private static readonly Color VerdantDark = Color.FromHtml("#186818");

    private static readonly Color AzureLight = Color.FromHtml("#50B8E0");
    private static readonly Color AzureMedium = Color.FromHtml("#2880B0");
    private static readonly Color AzureDark = Color.FromHtml("#105070");

    private static readonly Color SolarLight = Color.FromHtml("#F0D040");
    private static readonly Color SolarMedium = Color.FromHtml("#C8A018");
    private static readonly Color SolarDark = Color.FromHtml("#887008");

    private static readonly Color LumenLight = Color.FromHtml("#D0E8F8");
    private static readonly Color LumenMedium = Color.FromHtml("#88B8D8");
    private static readonly Color LumenDark = Color.FromHtml("#4878A0");

    private static readonly Color UmbraLight = Color.FromHtml("#B870D0");
    private static readonly Color UmbraMedium = Color.FromHtml("#8040A0");
    private static readonly Color UmbraDark = Color.FromHtml("#482868");

    private static (Color light, Color medium, Color dark) GetMineralColors(MineralType mineral)
    {
        return mineral switch
        {
            MineralType.Cinder => (CinderLight, CinderMedium, CinderDark),
            MineralType.Verdant => (VerdantLight, VerdantMedium, VerdantDark),
            MineralType.Azure => (AzureLight, AzureMedium, AzureDark),
            MineralType.Solar => (SolarLight, SolarMedium, SolarDark),
            MineralType.Lumen => (LumenLight, LumenMedium, LumenDark),
            MineralType.Umbra => (UmbraLight, UmbraMedium, UmbraDark),
            _ => (Colors.White, Colors.White, Colors.White),
        };
    }

    public static LevelColorPalette FromMinerals(MineralType primary, MineralType secondary)
    {
        var (priLight, priMedium, priDark) = GetMineralColors(primary);
        var (secLight, secMedium, secDark) = GetMineralColors(secondary);

        return new LevelColorPalette
        {
            PrimaryDark = priDark,
            PrimaryMedium = priMedium,
            PrimaryLight = priLight,
            SecondaryDark = secDark,
            SecondaryMedium = secMedium,
            SecondaryLight = secLight,
        };
    }
}
