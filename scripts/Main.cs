using Godot;

public partial class Main : Node
{
    public override void _Ready()
    {
        var terminalScene = GD.Load<PackedScene>("res://scenes/game/MissionTerminal.tscn");
        var terminal = terminalScene.Instantiate();
        AddChild(terminal);
    }
}
