using Godot;

public abstract partial class MissionLevel : Node2D
{
    public abstract ExtractionZone GetExtractionZone();

    public abstract void ApplyMission(MissionRunData mission);
}
