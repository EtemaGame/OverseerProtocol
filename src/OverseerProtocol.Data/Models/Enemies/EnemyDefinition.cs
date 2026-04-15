namespace OverseerProtocol.Data.Models.Enemies;

public sealed class EnemyDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? EnemyTypeName { get; set; }
    public int PowerLevel { get; set; }
    public int MaxCount { get; set; }
    public bool CanSpawnOutside { get; set; }
    public bool CanSpawnInside { get; set; }
    public bool IsDaytime { get; set; }
}
