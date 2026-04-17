using System.Collections.Generic;

namespace OverseerProtocol.Data.Models;

public sealed class MoonOverrideCollection
{
    public int SchemaVersion { get; set; } = 1;
    public List<MoonOverrideDefinition> Overrides { get; set; } = new();
}

public sealed class MoonOverrideDefinition
{
    public string MoonId { get; set; } = "";
    public int? RiskLevel { get; set; }
    public string? RiskLabel { get; set; }
    public int? RoutePrice { get; set; }
}
