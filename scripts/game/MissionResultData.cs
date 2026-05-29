using System.Collections.Generic;
using System.Linq;

public sealed class MissionResultData
{
    public bool Succeeded { get; set; }

    public string MissionTitle { get; set; } = string.Empty;

    public string ThemeLabel { get; set; } = string.Empty;

    public int DifficultyTier { get; set; }

    public int MaterialTarget { get; set; }

    public int TotalCollected { get; set; }

    public List<MissionModifierType> Modifiers { get; set; } = new();

    public Dictionary<string, int> CollectedMinerals { get; set; } = new();

    public string SummaryText
    {
        get
        {
            var parts = CollectedMinerals
                .Where(entry => entry.Value > 0)
                .Select(entry => $"{entry.Key}:{entry.Value}")
                .ToList();

            return parts.Count > 0 ? string.Join(" ", parts) : "None";
        }
    }

    public string ModifierSummary => Modifiers.Count > 0 ? string.Join(", ", Modifiers) : "None";
}
