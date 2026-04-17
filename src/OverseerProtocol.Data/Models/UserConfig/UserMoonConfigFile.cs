using System;
using System.Collections.Generic;
using OverseerProtocol.Data.Models.Spawns;

namespace OverseerProtocol.Data.Models.UserConfig;

public sealed class UserMoonConfigFile
{
    public int SchemaVersion { get; set; } = 1;
    public string GeneratedUtc { get; set; } = DateTime.UtcNow.ToString("O");
    public string MoonId { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool MissingFromRuntime { get; set; }
    public string Notes { get; set; } = "Edita override/spawns/scrap/items segun necesites. observed se regenera desde el juego.";
    public ObservedMoonConfig Observed { get; set; } = new();
    public MoonOverrideConfig Override { get; set; } = new();
    public MoonSpawnPoolsConfig Spawns { get; set; } = new();
    public MoonScrapConfig Scrap { get; set; } = new();
    public MoonItemPoolConfig Items { get; set; } = new();
}

public sealed class ObservedMoonConfig
{
    public string DisplayName { get; set; } = "";
    public string InternalName { get; set; } = "";
    public int LevelIndex { get; set; }
    public int RoutePrice { get; set; }
    public int RiskLevel { get; set; }
    public string RiskLabel { get; set; } = "";
}

public sealed class MoonOverrideConfig
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public int? RoutePrice { get; set; }
    public int? RiskLevel { get; set; }
    public string? RiskLabel { get; set; }
    public float? RoutePriceMultiplier { get; set; }
}

public sealed class MoonSpawnPoolsConfig
{
    public SpawnPoolConfig InsideEnemies { get; set; } = SpawnPoolConfig.Keep();
    public SpawnPoolConfig OutsideEnemies { get; set; } = SpawnPoolConfig.Keep();
    public SpawnPoolConfig DaytimeEnemies { get; set; } = SpawnPoolConfig.Keep();
}

public sealed class SpawnPoolConfig
{
    public string Mode { get; set; } = "keep";
    public List<SpawnEntry> Entries { get; set; } = new();

    public static SpawnPoolConfig Keep() =>
        new() { Mode = "keep" };
}

public sealed class MoonScrapConfig
{
    public int? MinScrapCount { get; set; }
    public int? MaxScrapCount { get; set; }
    public int? MinTotalScrapValue { get; set; }
    public int? MaxTotalScrapValue { get; set; }
    public float? ScrapAmountMultiplier { get; set; }
    public float? ScrapValueMultiplier { get; set; }
}

public sealed class MoonItemPoolConfig
{
    public string Mode { get; set; } = "keep";
    public List<MoonItemSpawnEntry> Entries { get; set; } = new();
}

public sealed class MoonItemSpawnEntry
{
    public string ItemId { get; set; } = "";
    public int Rarity { get; set; }
}
