public static class ModuleEffectResolver
{
    public static ResolvedModuleEffects Resolve(PlayerLoadout? loadout)
    {
        var effects = new ResolvedModuleEffects();

        if (loadout == null)
        {
            return effects;
        }

        foreach (var module in loadout.GetAllModules())
        {
            foreach (var property in module.Properties)
            {
                var definition = ModulePropertyCatalog.GetDefinition(property.PropertyId);
                var intensity = property.BaseRollValue + (property.RefinementLevel * 0.05f);

                switch (definition.DisplayName)
                {
                    case "Launch Height":
                        effects.LaunchHeightBonus += 32.0f * intensity;
                        break;

                    case "Burst Takeoff":
                        effects.BurstTakeoffSpeedBonus += 22.0f * intensity;
                        break;

                    case "Hang Time":
                        effects.HangTimeGravityMultiplier *= 1.0f - (0.18f * intensity);
                        break;

                    case "Rail Speed":
                        effects.RailSpeedBonus += 30.0f * intensity;
                        break;

                    case "Armor On Rail Entry":
                        effects.RailEntryArmorSeconds += 0.18f * intensity;
                        break;

                    case "Spark Trail":
                        effects.GrindContactDamage += 8.0f * intensity;
                        break;
                }
            }
        }

        effects.HangTimeGravityMultiplier = System.Math.Clamp(effects.HangTimeGravityMultiplier, 0.45f, 1.0f);
        return effects;
    }
}
