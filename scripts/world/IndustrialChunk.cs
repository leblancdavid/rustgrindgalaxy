using Godot;

public partial class IndustrialChunk : Node2D
{
    [Export] public int ChunkWidth = 128;

    public int GetChunkWidth()
    {
        return ChunkWidth;
    }
}
