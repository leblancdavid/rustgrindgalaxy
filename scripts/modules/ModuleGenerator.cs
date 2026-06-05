using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class ModuleGenerator
{
    private static readonly string[] FlipTrickNames =
    {
        "Kickflip",
        "Heelflip",
        "Hardflip",
        "Varial Flip",
        "Laser Flip",
    };

    private static readonly string[] GrabTrickNames =
    {
        "Indy Grab",
        "Mute Grab",
        "Melon Grab",
        "Stalefish",
        "Method Grab",
    };

    private readonly RandomNumberGenerator _rng;

    public ModuleGenerator(RandomNumberGenerator? rng = null)
    {
        _rng = rng ?? new RandomNumberGenerator();
        if (rng == null)
        {
            _rng.Randomize();
        }
    }

    public PlayerLoadout GenerateDebugLoadout(ModuleRarity rarity)
    {
        return new PlayerLoadout(
            Generate(ModuleType.Ollie, rarity),
            Generate(ModuleType.Grind, rarity),
            Generate(ModuleType.Flip, rarity),
            Generate(ModuleType.Grab, rarity));
    }

    public ModuleInstance Generate(ModuleType moduleType, ModuleRarity rarity)
    {
        var properties = rarity == ModuleRarity.Unique
            ? GenerateUniqueProperties(moduleType)
            : GenerateStandardProperties(moduleType, rarity);

        var module = new ModuleInstance(
            Guid.NewGuid().ToString("N"),
            moduleType,
            rarity,
            properties,
            GenerateDisplayName(moduleType));

        ModuleCategoryResolver.UpdateCategory(module);
        return module;
    }

    private List<RolledModuleProperty> GenerateStandardProperties(ModuleType moduleType, ModuleRarity rarity)
    {
        var propertyCount = GetPropertyCount(rarity);
        var availableDefinitions = ModulePropertyCatalog.GetDefinitions(moduleType).ToList();
        var rolledProperties = new List<RolledModuleProperty>(propertyCount);

        for (var i = 0; i < propertyCount; i++)
        {
            var index = _rng.RandiRange(0, availableDefinitions.Count - 1);
            var definition = availableDefinitions[index];
            availableDefinitions.RemoveAt(index);

            rolledProperties.Add(new RolledModuleProperty(definition.Id, RollBaseValue()));
        }

        return rolledProperties;
    }

    private List<RolledModuleProperty> GenerateUniqueProperties(ModuleType moduleType)
    {
        var rolledProperties = new List<RolledModuleProperty>();

        foreach (var mineral in Enum.GetValues<MineralType>())
        {
            var variant = (ModuleVariantType)_rng.RandiRange(0, Enum.GetValues<ModuleVariantType>().Length - 1);
            var definition = ModulePropertyCatalog.GetDefinition(moduleType, mineral, variant);
            rolledProperties.Add(new RolledModuleProperty(definition.Id, RollBaseValue()));
        }

        return rolledProperties;
    }

    private int GetPropertyCount(ModuleRarity rarity)
    {
        return rarity switch
        {
            ModuleRarity.Common => 1,
            ModuleRarity.Uncommon => 2,
            ModuleRarity.Rare => 3,
            ModuleRarity.Epic => 4,
            ModuleRarity.Legendary => 5,
            ModuleRarity.Unique => 6,
            _ => 1,
        };
    }

    private float RollBaseValue()
    {
        return Mathf.Round((0.85f + (_rng.Randf() * 0.30f)) * 100.0f) / 100.0f;
    }

    private string GenerateDisplayName(ModuleType moduleType)
    {
        return moduleType switch
        {
            ModuleType.Flip => PickName(FlipTrickNames),
            ModuleType.Grab => PickName(GrabTrickNames),
            ModuleType.Ollie => "Launch Core",
            ModuleType.Grind => "Rail Engine",
            _ => moduleType.ToString(),
        };
    }

    private string PickName(IReadOnlyList<string> names)
    {
        return names[_rng.RandiRange(0, names.Count - 1)];
    }
}
