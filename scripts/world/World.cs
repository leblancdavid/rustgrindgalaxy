using Godot;

public partial class World : Node2D
{
    public PlayerController Player { get; private set; } = null!;

    public PlayerLoadout DebugLoadout { get; private set; } = null!;

    public override void _Ready()
    {
        Player = GetNode<PlayerController>("Player");

        var generator = new ModuleGenerator();
        DebugLoadout = generator.GenerateDebugLoadout(ModuleRarity.Rare);
        Player.SetLoadout(DebugLoadout);

        GD.Print("Generated debug module loadout:");
        foreach (var module in DebugLoadout.GetAllModules())
        {
            GD.Print($" - {module.GetDebugSummary()}");
        }
    }
}
