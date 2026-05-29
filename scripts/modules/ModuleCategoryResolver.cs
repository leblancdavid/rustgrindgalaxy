using System;
using System.Collections.Generic;
using System.Linq;

public static class ModuleCategoryResolver
{
    public static void UpdateCategory(ModuleInstance module)
    {
        if (module.Rarity == ModuleRarity.Unique)
        {
            module.SetCategory(ModuleCategoryType.Prismatic, Enum.GetValues<MineralType>());
            return;
        }

        var groupedProperties = module.Properties
            .Select(property => new
            {
                Property = property,
                Definition = ModulePropertyCatalog.GetDefinition(property.PropertyId),
            })
            .GroupBy(entry => entry.Definition.Mineral)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Count = group.Count(),
                    RollTotal = group.Sum(entry => entry.Property.BaseRollValue),
                });

        var refinedMinerals = module.RefinementByMineral
            .Where(entry => entry.Value > 0)
            .Select(entry => entry.Key)
            .ToList();

        var candidateMinerals = refinedMinerals.Count > 0
            ? refinedMinerals
            : groupedProperties.Keys.ToList();

        if (candidateMinerals.Count == 0)
        {
            module.SetCategory(ModuleCategoryType.Pure, new[] { MineralType.Cinder });
            return;
        }

        var orderedMinerals = candidateMinerals
            .OrderByDescending(mineral => module.RefinementByMineral.GetValueOrDefault(mineral))
            .ThenByDescending(mineral => groupedProperties.GetValueOrDefault(mineral)?.Count ?? 0)
            .ThenByDescending(mineral => groupedProperties.GetValueOrDefault(mineral)?.RollTotal ?? 0.0f)
            .ThenBy(mineral => (int)mineral)
            .ToList();

        if (orderedMinerals.Count == 1)
        {
            module.SetCategory(ModuleCategoryType.Pure, orderedMinerals.Take(1));
            return;
        }

        if (orderedMinerals.Count == 2)
        {
            module.SetCategory(ModuleCategoryType.Alloy, orderedMinerals.Take(2));
            return;
        }

        module.SetCategory(ModuleCategoryType.Matrix, orderedMinerals.Take(3));
    }
}
