using Godot;

public partial class World : Node2D
{
    public PlayerController Player { get; private set; } = null!;

    public override void _Ready()
    {
        Player = GetNode<PlayerController>("Player");
    }
}
