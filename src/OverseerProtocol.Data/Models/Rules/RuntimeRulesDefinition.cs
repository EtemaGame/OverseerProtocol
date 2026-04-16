using System.Collections.Generic;

namespace OverseerProtocol.Data.Models.Rules;

public sealed class RuntimeRulesDefinition
{
    public int SchemaVersion { get; set; } = 1;
    public EconomyRulesDefinition Economy { get; set; } = new();
    public ShipRulesDefinition Ship { get; set; } = new();
    public WeatherRulesDefinition Weather { get; set; } = new();
    public Dictionary<string, MoonRuntimeRuleDefinition> MoonRules { get; set; } = new();
}

public sealed class EconomyRulesDefinition
{
    public float QuotaMultiplier { get; set; } = 1f;
    public float DeadlineMultiplier { get; set; } = 1f;
    public float TravelDiscountMultiplier { get; set; } = 1f;
    public float ScrapValueMultiplier { get; set; } = 1f;
    public bool PreserveShipLootOnTeamWipe { get; set; }
}

public sealed class ShipRulesDefinition
{
    public float LandingSpeedMultiplier { get; set; } = 1f;
    public float DropshipSpeedMultiplier { get; set; } = 1f;
    public float ScannerDistanceMultiplier { get; set; } = 1f;
    public float BatteryCapacityMultiplier { get; set; } = 1f;
}

public sealed class WeatherRulesDefinition
{
    public float ClearRewardMultiplier { get; set; } = 1f;
    public float RainyRewardMultiplier { get; set; } = 1f;
    public float StormyRewardMultiplier { get; set; } = 1f;
    public float FoggyRewardMultiplier { get; set; } = 1f;
    public float FloodedRewardMultiplier { get; set; } = 1f;
    public float EclipsedRewardMultiplier { get; set; } = 1f;
}

public sealed class MoonRuntimeRuleDefinition
{
    public float RoutePriceMultiplier { get; set; } = 1f;
    public float ScrapValueMultiplier { get; set; } = 1f;
    public float SpawnRarityMultiplier { get; set; } = 1f;
    public float WeatherRewardMultiplier { get; set; } = 1f;
}
