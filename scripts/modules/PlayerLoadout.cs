using System.Collections.Generic;

public sealed class PlayerLoadout
{
    public PlayerLoadout(ModuleInstance ollie, ModuleInstance grind, ModuleInstance flip, ModuleInstance grab)
    {
        Ollie = ollie;
        Grind = grind;
        Flip = flip;
        Grab = grab;
    }

    public ModuleInstance Ollie { get; }

    public ModuleInstance Grind { get; }

    public ModuleInstance Flip { get; }

    public ModuleInstance Grab { get; }

    public IEnumerable<ModuleInstance> GetAllModules()
    {
        yield return Ollie;
        yield return Grind;
        yield return Flip;
        yield return Grab;
    }

    public ModuleInstance GetModule(ModuleType moduleType)
    {
        return moduleType switch
        {
            ModuleType.Ollie => Ollie,
            ModuleType.Grind => Grind,
            ModuleType.Flip => Flip,
            ModuleType.Grab => Grab,
            _ => Ollie,
        };
    }
}
