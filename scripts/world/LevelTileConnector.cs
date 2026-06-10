using Godot;

public struct LevelTileConnector
{
    public float GroundY;
    public float RailY;

    public bool HasRail => RailY >= 0.0f;

    public static bool AreCompatible(LevelTileConnector left, LevelTileConnector right, float tolerance = 0.01f)
    {
        return Mathf.Abs(left.GroundY - right.GroundY) <= tolerance;
    }
}
