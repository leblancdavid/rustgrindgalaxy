using System;
using System.Collections.Generic;
using System.Linq;

public static class ModulePropertyCatalog
{
    private static readonly IReadOnlyList<ModulePropertyDefinition> Definitions;
    private static readonly Dictionary<string, ModulePropertyDefinition> DefinitionsById;
    private static readonly Dictionary<ModuleType, IReadOnlyList<ModulePropertyDefinition>> DefinitionsByModuleType;
    private static readonly Dictionary<(ModuleType, MineralType, ModuleVariantType), ModulePropertyDefinition> DefinitionsByBucket;

    static ModulePropertyCatalog()
    {
        var definitions = BuildDefinitions();

        if (definitions.Count != 72)
        {
            throw new InvalidOperationException($"Expected 72 module properties but found {definitions.Count}.");
        }

        DefinitionsById = definitions.ToDictionary(definition => definition.Id, definition => definition);
        DefinitionsByBucket = definitions.ToDictionary(
            definition => (definition.ModuleType, definition.Mineral, definition.Variant),
            definition => definition);
        DefinitionsByModuleType = definitions
            .GroupBy(definition => definition.ModuleType)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<ModulePropertyDefinition>)group.ToList());
        Definitions = definitions;
    }

    public static IReadOnlyList<ModulePropertyDefinition> GetDefinitions(ModuleType moduleType)
    {
        return DefinitionsByModuleType[moduleType];
    }

    public static ModulePropertyDefinition GetDefinition(string propertyId)
    {
        return DefinitionsById[propertyId];
    }

    public static ModulePropertyDefinition GetDefinition(ModuleType moduleType, MineralType mineral, ModuleVariantType variant)
    {
        return DefinitionsByBucket[(moduleType, mineral, variant)];
    }

    public static string GetMineralDisplayName(MineralType mineral)
    {
        return mineral.ToString();
    }

    private static List<ModulePropertyDefinition> BuildDefinitions()
    {
        var definitions = new List<ModulePropertyDefinition>();

        Add(ModuleType.Ollie, MineralType.Cinder, ModuleVariantType.Offensive, DamageType.Fire, "Landing Shockwave", "Ollie landings create a short fire burst that damages nearby enemies.");
        Add(ModuleType.Ollie, MineralType.Cinder, ModuleVariantType.Defensive, DamageType.None, "Heat Shield Landing", "Gain brief damage reduction after landing from an ollie.");
        Add(ModuleType.Ollie, MineralType.Cinder, ModuleVariantType.Utility, DamageType.Fire, "Breach Kick", "Hard landings break weak hazards or crates in a small radius.");
        Add(ModuleType.Ollie, MineralType.Verdant, ModuleVariantType.Offensive, DamageType.Acid, "Spore Kickback", "Landing near enemies releases an acid pulse that lightly damages and disrupts them.");
        Add(ModuleType.Ollie, MineralType.Verdant, ModuleVariantType.Defensive, DamageType.None, "Recovery Pulse", "Clean ollie landings restore a small amount of health.");
        Add(ModuleType.Ollie, MineralType.Verdant, ModuleVariantType.Utility, DamageType.None, "Salvage Step", "Successful ollie landings increase pickup pull and salvage collection briefly.");
        Add(ModuleType.Ollie, MineralType.Azure, ModuleVariantType.Offensive, DamageType.Cold, "Frost Launch Edge", "The initial ollie rise lightly damages close enemies with a cold burst.");
        Add(ModuleType.Ollie, MineralType.Azure, ModuleVariantType.Defensive, DamageType.None, "Air Correction Window", "Gain a brief period of improved airborne recovery after ollie launch.");
        Add(ModuleType.Ollie, MineralType.Azure, ModuleVariantType.Utility, DamageType.None, "Launch Height", "Increases ollie height.");
        Add(ModuleType.Ollie, MineralType.Solar, ModuleVariantType.Offensive, DamageType.Shock, "Momentum Discharge", "Faster ollies convert movement speed into bonus shock impact on landing.");
        Add(ModuleType.Ollie, MineralType.Solar, ModuleVariantType.Defensive, DamageType.None, "Evasive Lift", "Gain a short evade window immediately after ollie takeoff.");
        Add(ModuleType.Ollie, MineralType.Solar, ModuleVariantType.Utility, DamageType.None, "Burst Takeoff", "Grants a short speed boost when performing an ollie.");
        Add(ModuleType.Ollie, MineralType.Lumen, ModuleVariantType.Offensive, DamageType.Radiant, "Precision Landing Pulse", "Perfect or clean landings emit a small radiant pulse that damages nearby enemies.");
        Add(ModuleType.Ollie, MineralType.Lumen, ModuleVariantType.Defensive, DamageType.None, "Safe Landing Plating", "Reduces damage or stagger taken during the landing window.");
        Add(ModuleType.Ollie, MineralType.Lumen, ModuleVariantType.Utility, DamageType.None, "Stabilized Touchdown", "Improves landing stability and shortens recovery after ollie landing.");
        Add(ModuleType.Ollie, MineralType.Umbra, ModuleVariantType.Offensive, DamageType.Void, "Void Drop", "Ollie landings release a void burst that deals higher damage when the player is at low health.");
        Add(ModuleType.Ollie, MineralType.Umbra, ModuleVariantType.Defensive, DamageType.None, "Siphon Impact Guard", "Landing on or near enemies grants a small temporary shield.");
        Add(ModuleType.Ollie, MineralType.Umbra, ModuleVariantType.Utility, DamageType.None, "Ghost Descent", "Slightly softens landing commitment and improves repositioning after descent.");

        Add(ModuleType.Grind, MineralType.Cinder, ModuleVariantType.Offensive, DamageType.Fire, "Spark Trail", "Grinding deals fire damage to enemies touched along the rail.");
        Add(ModuleType.Grind, MineralType.Cinder, ModuleVariantType.Defensive, DamageType.None, "Tempered Sparks", "Gain brief contact damage reduction while actively grinding.");
        Add(ModuleType.Grind, MineralType.Cinder, ModuleVariantType.Utility, DamageType.Fire, "Furnace Exit", "Exiting a rail creates a small burst that clears weak hazards near the exit.");
        Add(ModuleType.Grind, MineralType.Verdant, ModuleVariantType.Offensive, DamageType.Acid, "Corrosive Rail Wake", "Grinding leaves a short acid trail that damages enemies passing through it.");
        Add(ModuleType.Grind, MineralType.Verdant, ModuleVariantType.Defensive, DamageType.None, "Rail Repair", "Regenerate a small amount of health while maintaining a grind.");
        Add(ModuleType.Grind, MineralType.Verdant, ModuleVariantType.Utility, DamageType.None, "Salvage Sweep", "Increase pickup radius and resource collection while grinding.");
        Add(ModuleType.Grind, MineralType.Azure, ModuleVariantType.Offensive, DamageType.Cold, "Cryo Rail Arc", "Rail contact emits small cold jolts that damage nearby airborne enemies.");
        Add(ModuleType.Grind, MineralType.Azure, ModuleVariantType.Defensive, DamageType.None, "Coolflow Recovery", "Recover control more safely when leaving a rail unexpectedly.");
        Add(ModuleType.Grind, MineralType.Azure, ModuleVariantType.Utility, DamageType.None, "Glide Transfer", "Improves aerial transfer and directional control when entering or leaving rails.");
        Add(ModuleType.Grind, MineralType.Solar, ModuleVariantType.Offensive, DamageType.Shock, "Overcharge Rail", "Grinding at speed builds shock damage that discharges on enemy contact.");
        Add(ModuleType.Grind, MineralType.Solar, ModuleVariantType.Defensive, DamageType.None, "Voltage Buffer", "Gain brief protection against interruption when entering a rail.");
        Add(ModuleType.Grind, MineralType.Solar, ModuleVariantType.Utility, DamageType.None, "Rail Speed", "Increases movement speed while grinding.");
        Add(ModuleType.Grind, MineralType.Lumen, ModuleVariantType.Offensive, DamageType.Radiant, "Radiant Edge Rail", "Clean grind lines emit small radiant pulses that damage nearby enemies.");
        Add(ModuleType.Grind, MineralType.Lumen, ModuleVariantType.Defensive, DamageType.None, "Armor On Rail Entry", "Grants brief armor or damage reduction when entering a rail.");
        Add(ModuleType.Grind, MineralType.Lumen, ModuleVariantType.Utility, DamageType.None, "Mag Lock Stability", "Makes grind retention and exit timing more forgiving.");
        Add(ModuleType.Grind, MineralType.Umbra, ModuleVariantType.Offensive, DamageType.Void, "Void Shred Line", "Grinding through enemies deals void damage and briefly weakens them.");
        Add(ModuleType.Grind, MineralType.Umbra, ModuleVariantType.Defensive, DamageType.None, "Siphon Rail Guard", "Enemy contact during a grind grants a small shield or life siphon effect.");
        Add(ModuleType.Grind, MineralType.Umbra, ModuleVariantType.Utility, DamageType.None, "Threat Blur", "Grinding lowers enemy accuracy or threat focus on the player for a short time.");

        Add(ModuleType.Flip, MineralType.Cinder, ModuleVariantType.Offensive, DamageType.Fire, "Flip Impact", "Flip attacks deal increased fire damage on hit.");
        Add(ModuleType.Flip, MineralType.Cinder, ModuleVariantType.Defensive, DamageType.None, "Ember Guard Frame", "Gain a brief damage reduction window during flip startup.");
        Add(ModuleType.Flip, MineralType.Cinder, ModuleVariantType.Utility, DamageType.None, "Aggro Flip Carry", "Successful flip hits preserve more forward momentum.");
        Add(ModuleType.Flip, MineralType.Verdant, ModuleVariantType.Offensive, DamageType.Acid, "Caustic Slice", "Flip hits apply an acid burst that damages enemies over a short duration.");
        Add(ModuleType.Flip, MineralType.Verdant, ModuleVariantType.Defensive, DamageType.None, "Repair On Hit", "Successful flip hits restore a small amount of health.");
        Add(ModuleType.Flip, MineralType.Verdant, ModuleVariantType.Utility, DamageType.None, "Scrap Shear", "Enemies hit by flips have an increased chance to drop salvage or pickups.");
        Add(ModuleType.Flip, MineralType.Azure, ModuleVariantType.Offensive, DamageType.Cold, "Arc Width", "Increases the hit area or reach of flip attacks with a cold edge.");
        Add(ModuleType.Flip, MineralType.Azure, ModuleVariantType.Defensive, DamageType.None, "Drift Guard", "Improves air control during and immediately after a flip.");
        Add(ModuleType.Flip, MineralType.Azure, ModuleVariantType.Utility, DamageType.None, "Rotation Control", "Makes flip timing and aerial repositioning more controllable.");
        Add(ModuleType.Flip, MineralType.Solar, ModuleVariantType.Offensive, DamageType.Shock, "Combo Charge", "Successful flip hits generate additional ultimate or combo meter with shock output.");
        Add(ModuleType.Flip, MineralType.Solar, ModuleVariantType.Defensive, DamageType.None, "Quick Reset", "Shortens vulnerable recovery after completing a flip.");
        Add(ModuleType.Flip, MineralType.Solar, ModuleVariantType.Utility, DamageType.None, "Speed Loop", "Flip completion grants a short burst of movement speed.");
        Add(ModuleType.Flip, MineralType.Lumen, ModuleVariantType.Offensive, DamageType.Radiant, "Radiant Cutline", "Flip hits emit a narrow radiant follow-through pulse.");
        Add(ModuleType.Flip, MineralType.Lumen, ModuleVariantType.Defensive, DamageType.None, "Guard Frame", "Grants a small shield or guard window during the flip's active sequence.");
        Add(ModuleType.Flip, MineralType.Lumen, ModuleVariantType.Utility, DamageType.None, "Precision Arc", "Improves flip consistency and clean hit alignment.");
        Add(ModuleType.Flip, MineralType.Umbra, ModuleVariantType.Offensive, DamageType.Void, "Void Execution", "Flip hits deal bonus void damage to low-health enemies.");
        Add(ModuleType.Flip, MineralType.Umbra, ModuleVariantType.Defensive, DamageType.None, "Siphon Veil", "Successful flip hits grant a brief shield or damage siphon effect.");
        Add(ModuleType.Flip, MineralType.Umbra, ModuleVariantType.Utility, DamageType.None, "Threat Cut", "Flip hits reduce enemy aggression or targeting for a short time.");

        Add(ModuleType.Grab, MineralType.Cinder, ModuleVariantType.Offensive, DamageType.Fire, "Impact Release", "Releasing a grab into landing causes a small fire burst around the player.");
        Add(ModuleType.Grab, MineralType.Cinder, ModuleVariantType.Defensive, DamageType.None, "Heated Brace", "Gain brief resistance during grab release and landing.");
        Add(ModuleType.Grab, MineralType.Cinder, ModuleVariantType.Utility, DamageType.Fire, "Burnthrough Drop", "Grab releases destroy weak obstacles or hazards on landing.");
        Add(ModuleType.Grab, MineralType.Verdant, ModuleVariantType.Offensive, DamageType.Acid, "Acid Vent Release", "Grab release emits a small acid pulse that damages nearby enemies.");
        Add(ModuleType.Grab, MineralType.Verdant, ModuleVariantType.Defensive, DamageType.None, "Repair Grip", "Restore a small amount of health while holding or cleanly releasing a grab.");
        Add(ModuleType.Grab, MineralType.Verdant, ModuleVariantType.Utility, DamageType.None, "Scavenger Hold", "Grabs improve pickup attraction and salvage collection briefly.");
        Add(ModuleType.Grab, MineralType.Azure, ModuleVariantType.Offensive, DamageType.Cold, "Cold Wake Release", "Grab release emits a small cold burst that damages and lightly slows enemies.");
        Add(ModuleType.Grab, MineralType.Azure, ModuleVariantType.Defensive, DamageType.None, "Softfall Field", "Reduces descent risk and improves safe aerial correction during a grab.");
        Add(ModuleType.Grab, MineralType.Azure, ModuleVariantType.Utility, DamageType.None, "Hang Time", "Slows fall speed while the grab is active.");
        Add(ModuleType.Grab, MineralType.Solar, ModuleVariantType.Offensive, DamageType.Shock, "Shock Snap Release", "Releasing a grab at speed emits a short shock burst on landing.");
        Add(ModuleType.Grab, MineralType.Solar, ModuleVariantType.Defensive, DamageType.None, "Momentum Cushion", "Retains control and reduces punishment when transitioning out of a grab.");
        Add(ModuleType.Grab, MineralType.Solar, ModuleVariantType.Utility, DamageType.None, "Momentum Preserve", "Preserves more horizontal speed when releasing a grab.");
        Add(ModuleType.Grab, MineralType.Lumen, ModuleVariantType.Offensive, DamageType.Radiant, "Radiant Flare Release", "Clean grab release emits a small radiant pulse on landing.");
        Add(ModuleType.Grab, MineralType.Lumen, ModuleVariantType.Defensive, DamageType.None, "Shield Hold", "Grants brief shielding or damage reduction while holding a grab.");
        Add(ModuleType.Grab, MineralType.Lumen, ModuleVariantType.Utility, DamageType.None, "Stable Float", "Improves aerial steadiness and precision while grabbing.");
        Add(ModuleType.Grab, MineralType.Umbra, ModuleVariantType.Offensive, DamageType.Void, "Void Drop Field", "Grab release creates a small void burst that deals bonus damage in risky close range.");
        Add(ModuleType.Grab, MineralType.Umbra, ModuleVariantType.Defensive, DamageType.None, "Dark Siphon Hold", "Holding or releasing a grab near enemies grants a small shield or siphon effect.");
        Add(ModuleType.Grab, MineralType.Umbra, ModuleVariantType.Utility, DamageType.None, "Threat Fade", "Grabs briefly reduce enemy awareness or pressure after release.");

        return definitions;

        void Add(
            ModuleType moduleType,
            MineralType mineral,
            ModuleVariantType variant,
            DamageType damageType,
            string displayName,
            string description)
        {
            var id = $"{moduleType.ToString().ToLowerInvariant()}_{mineral.ToString().ToLowerInvariant()}_{variant.ToString().ToLowerInvariant()}_{ToSnakeCase(displayName)}";
            definitions.Add(new ModulePropertyDefinition(id, displayName, moduleType, mineral, variant, damageType, description));
        }
    }

    private static string ToSnakeCase(string text)
    {
        var chars = text
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray();

        return new string(chars)
            .Replace("__", "_")
            .Trim('_');
    }
}
