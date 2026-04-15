namespace OverseerProtocol.Data.Models.Enemies;

public sealed class EnemyDefinition
{
    public string Id { get; set; } = "";
    public string InternalName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int PowerLevel { get; set; }
    public int MaxCount { get; set; }
    
    // Intrinsic Type Flags
    public bool IsOutsideEnemy { get; set; }
    public bool IsDaytimeEnemy { get; set; }
    
    // Observed Data from Moon Pools
    public bool ObservedInside { get; set; }
    public bool ObservedOutside { get; set; }
    public bool ObservedDaytime { get; set; }
}
