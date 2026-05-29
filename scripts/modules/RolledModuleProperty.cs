public sealed class RolledModuleProperty
{
    public RolledModuleProperty(string propertyId, float baseRollValue, int refinementLevel = 0)
    {
        PropertyId = propertyId;
        BaseRollValue = baseRollValue;
        RefinementLevel = refinementLevel;
    }

    public string PropertyId { get; }

    public float BaseRollValue { get; }

    public int RefinementLevel { get; private set; }

    public void SetRefinementLevel(int refinementLevel)
    {
        RefinementLevel = refinementLevel;
    }
}
