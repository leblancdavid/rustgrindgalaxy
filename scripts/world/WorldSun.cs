using Godot;

/// <summary>
/// Per-level sun direction. Only one level is active at a time, so a static
/// holder is enough. Set once at level init from the level seed; shadows read
/// it every frame. Positive angle = sun leans to the right = shadows skew left.
/// </summary>
public static class WorldSun
{
    public const float MinAngle = -45.0f;
    public const float MaxAngle = 45.0f;

    // Gentle default so shadows have direction even in scenes with no level generator.
    private const float DefaultAngle = 24.0f;

    public static float AngleDegrees { get; private set; } = DefaultAngle;
    public static float Shear { get; private set; } = ShearForAngle(DefaultAngle);

    public static void SetFromSeed(ulong seed)
    {
        var rng = new RandomNumberGenerator { Seed = seed };
        SetAngle(rng.RandfRange(MinAngle, MaxAngle));
    }

    public static void SetAngle(float degrees)
    {
        AngleDegrees = degrees;
        Shear = ShearForAngle(degrees);
    }

    public static float ShearForAngle(float degrees)
    {
        return -Mathf.Tan(Mathf.DegToRad(degrees));
    }
}
