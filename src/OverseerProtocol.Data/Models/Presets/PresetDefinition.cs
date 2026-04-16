using System.Collections.Generic;

namespace OverseerProtocol.Data.Models.Presets;

public sealed class PresetDefinition
{
    public int SchemaVersion { get; set; } = 1;
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "OverseerProtocol";
    public string MinimumOverseerVersion { get; set; } = "0.1.0";
    public string CompatibleGameVersion { get; set; } = "";
    public float? ItemWeightMultiplier { get; set; }
    public float? SpawnRarityMultiplier { get; set; }
    public float? RoutePriceMultiplier { get; set; }
    public List<string> Notes { get; set; } = new();
}
