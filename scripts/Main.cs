using Godot;

public partial class Main : Node
{
    public override void _Ready()
    {
        var procGenTestScene = GD.Load<PackedScene>("res://scenes/world/ProcGenTest.tscn");
        var procGenTest = procGenTestScene.Instantiate();
        AddChild(procGenTest);
    }
}
