using Godot;

public partial class LevelTile : Node2D
{
    [Export] public float TileWidth = 1280.0f;
    [Export] public float LeftGroundY = 164.0f;
    [Export] public float LeftRailY = -1.0f;
    [Export] public float RightGroundY = 164.0f;
    [Export] public float RightRailY = -1.0f;

    public LevelTileConnector GetLeftConnector()
    {
        return new LevelTileConnector { GroundY = LeftGroundY, RailY = LeftRailY };
    }

    public LevelTileConnector GetRightConnector()
    {
        return new LevelTileConnector { GroundY = RightGroundY, RailY = RightRailY };
    }

    public float GetTileLeftX()
    {
        return Scale.X < 0 ? Position.X - TileWidth : Position.X;
    }

    public float GetTileRightX()
    {
        return Scale.X < 0 ? Position.X : Position.X + TileWidth;
    }
}
