using Godot;

public partial class Main : Node
{
    public override void _Ready()
    {
        var gameScene = GD.Load<PackedScene>("res://scenes/game/Game.tscn");
        var game = gameScene.Instantiate();
        AddChild(game);
    }
}
