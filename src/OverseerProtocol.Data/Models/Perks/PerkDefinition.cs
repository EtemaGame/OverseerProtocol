using System.Collections.Generic;

namespace OverseerProtocol.Data.Models.Perks;

public sealed class PerkCatalogDefinition
{
    public int SchemaVersion { get; set; } = 1;
    public List<PerkDefinition> PlayerPerks { get; set; } = new();
    public List<PerkDefinition> ShipPerks { get; set; } = new();
}

public sealed class PerkDefinition
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public int MaxRank { get; set; }
    public List<int> RankCosts { get; set; } = new();
    public List<PerkRankEffect> Effects { get; set; } = new();
}

public sealed class PerkRankEffect
{
    public int Rank { get; set; }
    public string Stat { get; set; } = "";
    public float Multiplier { get; set; } = 1f;
    public float Additive { get; set; }
}
