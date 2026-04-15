namespace OverseerProtocol.Data.Models.Items;

public sealed class ItemDefinition
{
    public string Id { get; set; } = "";
    public string InternalName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Category { get; set; }
    public int CreditsWorth { get; set; }
    public float Weight { get; set; }
    public bool IsScrap { get; set; }
    public bool IsConductiveMetal { get; set; }
    public bool RequiresBattery { get; set; }
    public int MaxStack { get; set; }
    public string? SpawnPrefabName { get; set; }
}
