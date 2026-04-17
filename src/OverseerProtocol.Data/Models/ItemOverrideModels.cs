using System.Collections.Generic;

namespace OverseerProtocol.Data.Models;

public sealed class ItemOverrideCollection
{
    public int SchemaVersion { get; set; } = 1;
    public List<ItemOverrideDefinition> Overrides { get; set; } = new();
}

public sealed class ItemOverrideDefinition
{
    public string Id { get; set; } = "";
    public float? Weight { get; set; }
    public int? CreditsWorth { get; set; }
    public bool? AddToStore { get; set; }
    public int? StorePrice { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
}
