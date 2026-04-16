using System.Collections.Generic;

namespace OverseerProtocol.Data.Models.Presets;

public sealed class PresetDefinition
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public float? ItemWeightMultiplier { get; set; }
    public float? SpawnRarityMultiplier { get; set; }
    public List<string> Notes { get; set; } = new();
}
