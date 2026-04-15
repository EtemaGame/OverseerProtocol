namespace OverseerProtocol.Data.Models.Moons;

public sealed class MoonDefinition
{
    public string Id { get; set; } = "";
    public string InternalName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int RiskLevel { get; set; }
}
