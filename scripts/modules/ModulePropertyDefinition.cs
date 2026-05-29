public sealed class ModulePropertyDefinition
{
    public ModulePropertyDefinition(
        string id,
        string displayName,
        ModuleType moduleType,
        MineralType mineral,
        ModuleVariantType variant,
        DamageType damageType,
        string description,
        bool sliceReady = true)
    {
        Id = id;
        DisplayName = displayName;
        ModuleType = moduleType;
        Mineral = mineral;
        Variant = variant;
        DamageType = damageType;
        Description = description;
        SliceReady = sliceReady;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public ModuleType ModuleType { get; }

    public MineralType Mineral { get; }

    public ModuleVariantType Variant { get; }

    public DamageType DamageType { get; }

    public string Description { get; }

    public bool SliceReady { get; }
}
