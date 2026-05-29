using Godot;

public partial class Main : Node
{
    public override void _Ready()
    {
        var movementTestScene = GD.Load<PackedScene>("res://scenes/world/MovementTest.tscn");
        var movementTest = movementTestScene.Instantiate();
        AddChild(movementTest);
    }
}
