using System;
using System.Collections.Generic;

namespace OverseerProtocol.Data.Models.Progression;

public sealed class ProgressionData
{
    public int SchemaVersion { get; set; } = 1;
    public string SaveId { get; set; } = "default";
    public string ActivePreset { get; set; } = "default";
    public string LastUpdatedUtc { get; set; } = DateTime.UtcNow.ToString("O");
    public ShipProgressionData Ship { get; set; } = new();
    public List<PlayerProgressionData> Players { get; set; } = new();
}

public sealed class ShipProgressionData
{
    public int Level { get; set; }
    public int Experience { get; set; }
    public int UnspentPoints { get; set; }
    public Dictionary<string, int> PerkRanks { get; set; } = new();
}

public sealed class PlayerProgressionData
{
    public string PlayerId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int Level { get; set; }
    public int Experience { get; set; }
    public int UnspentPoints { get; set; }
    public int RespecCount { get; set; }
    public Dictionary<string, int> PerkRanks { get; set; } = new();
}
