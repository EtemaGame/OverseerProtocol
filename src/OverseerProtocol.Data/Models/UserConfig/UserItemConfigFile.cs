using System;
using System.Collections.Generic;

namespace OverseerProtocol.Data.Models.UserConfig;

public sealed class UserItemConfigFile
{
    public int SchemaVersion { get; set; } = 1;
    public string GeneratedUtc { get; set; } = DateTime.UtcNow.ToString("O");
    public string Notes { get; set; } = "Edita override/store/battery/spawn segun necesites. observed se regenera desde el juego.";
    public List<UserItemConfig> Items { get; set; } = new();
}

public sealed class UserItemConfig
{
    public string Id { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool MissingFromRuntime { get; set; }
    public ObservedItemConfig Observed { get; set; } = new();
    public ItemOverrideConfig Override { get; set; } = new();
    public ItemStoreConfig Store { get; set; } = new();
    public ItemBatteryConfig Battery { get; set; } = new();
    public ItemSpawnConfig Spawn { get; set; } = new();
}

public sealed class ObservedItemConfig
{
    public string DisplayName { get; set; } = "";
    public string InternalName { get; set; } = "";
    public int CreditsWorth { get; set; }
    public float Weight { get; set; }
    public bool IsScrap { get; set; }
    public bool IsConductiveMetal { get; set; }
    public bool RequiresBattery { get; set; }
    public string? SpawnPrefabName { get; set; }
}

public sealed class ItemOverrideConfig
{
    public int? CreditsWorth { get; set; }
    public float? Weight { get; set; }
}

public sealed class ItemStoreConfig
{
    public bool? AddToStore { get; set; }
    public int? StorePrice { get; set; }
    public int? MaxStoreStock { get; set; }
}

public sealed class ItemBatteryConfig
{
    public bool? RequiresBattery { get; set; }
    public float? BatteryUsageMultiplier { get; set; }
    public float? BatteryCapacityMultiplier { get; set; }
}

public sealed class ItemSpawnConfig
{
    public bool? AllowAsScrap { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
}
