using System.Collections.Generic;
using System.Linq;

public sealed class ModuleInstance
{
    private readonly List<RolledModuleProperty> _properties;
    private readonly List<MineralType> _categoryMinerals = new();

    public ModuleInstance(
        string instanceId,
        ModuleType moduleType,
        ModuleRarity rarity,
        IEnumerable<RolledModuleProperty> properties,
        string displayName,
        IDictionary<MineralType, int>? refinementByMineral = null)
    {
        InstanceId = instanceId;
        ModuleType = moduleType;
        Rarity = rarity;
        DisplayName = displayName;
        _properties = properties.ToList();
        RefinementByMineral = refinementByMineral != null
            ? new Dictionary<MineralType, int>(refinementByMineral)
            : new Dictionary<MineralType, int>();

        foreach (var mineral in System.Enum.GetValues<MineralType>())
        {
            RefinementByMineral.TryAdd(mineral, 0);
        }
    }

    public string InstanceId { get; }

    public ModuleType ModuleType { get; }

    public ModuleRarity Rarity { get; }

    public string DisplayName { get; }

    public IReadOnlyList<RolledModuleProperty> Properties => _properties;

    public Dictionary<MineralType, int> RefinementByMineral { get; }

    public ModuleCategoryType CategoryType { get; private set; } = ModuleCategoryType.Pure;

    public IReadOnlyList<MineralType> CategoryMinerals => _categoryMinerals;

    public void SetCategory(ModuleCategoryType categoryType, IEnumerable<MineralType> minerals)
    {
        CategoryType = categoryType;
        _categoryMinerals.Clear();
        _categoryMinerals.AddRange(minerals);
    }

    public string GetCategoryName()
    {
        if (CategoryType == ModuleCategoryType.Prismatic)
        {
            return "Prismatic";
        }

        var names = CategoryMinerals.Select(ModulePropertyCatalog.GetMineralDisplayName).ToList();

        return CategoryType switch
        {
            ModuleCategoryType.Pure when names.Count > 0 => $"Pure {names[0]}",
            ModuleCategoryType.Alloy when names.Count > 1 => $"{names[0]}-{names[1]} Alloy",
            ModuleCategoryType.Matrix when names.Count > 2 => $"{names[0]}-{names[1]}-{names[2]} Matrix",
            _ => "Uncategorized",
        };
    }

    public string GetDebugSummary()
    {
        var properties = Properties
            .Select(property =>
            {
                var definition = ModulePropertyCatalog.GetDefinition(property.PropertyId);
                return $"{definition.DisplayName} [{ModulePropertyCatalog.GetMineralDisplayName(definition.Mineral)} {definition.Variant}] x{property.BaseRollValue:0.00}";
            });

        return $"{Rarity} {DisplayName} [{ModuleType}] | {GetCategoryName()} | {string.Join(", ", properties)}";
    }
}
