using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models.Rules;
using OverseerProtocol.GameAbstractions.Overrides;

namespace OverseerProtocol.Features;

public sealed class RuntimeRulesFeature
{
    private readonly RuntimeRulesApplier _applier = new();

    public RuntimeRulesDefinition LoadOrCreate()
    {
        var rules = new RuntimeTuningConfig(OPConfig.ConfigFile).BuildRuntimeRules();

        Normalize(rules);
        OPLog.Info("RuntimeRules", $"Loaded runtime rules from .cfg: travelDiscount={rules.Economy.TravelDiscountMultiplier:0.###}, moonRules={rules.MoonRules.Count}");
        return rules;
    }

    public void Apply(RuntimeRulesDefinition rules)
    {
        OPLog.Info("RuntimeRules", "Applying runtime rules from .cfg. Active today: travelDiscountMultiplier and per-moon routePriceMultiplier.");
        _applier.Apply(rules);
    }

    private static void Normalize(RuntimeRulesDefinition rules)
    {
        if (rules.SchemaVersion <= 0)
        {
            OPLog.Info("RuntimeRules", $"Normalizing schemaVersion {rules.SchemaVersion} -> 1");
            rules.SchemaVersion = 1;
        }

        rules.Economy ??= new EconomyRulesDefinition();
        rules.Ship ??= new ShipRulesDefinition();
        rules.Weather ??= new WeatherRulesDefinition();
        rules.MoonRules ??= new System.Collections.Generic.Dictionary<string, MoonRuntimeRuleDefinition>();

        rules.Economy.QuotaMultiplier = NormalizeMultiplier(rules.Economy.QuotaMultiplier, "economy.quotaMultiplier");
        rules.Economy.DeadlineMultiplier = NormalizeMultiplier(rules.Economy.DeadlineMultiplier, "economy.deadlineMultiplier");
        rules.Economy.TravelDiscountMultiplier = NormalizeMultiplier(rules.Economy.TravelDiscountMultiplier, "economy.travelDiscountMultiplier");
        rules.Economy.ScrapValueMultiplier = NormalizeMultiplier(rules.Economy.ScrapValueMultiplier, "economy.scrapValueMultiplier");

        rules.Ship.LandingSpeedMultiplier = NormalizeMultiplier(rules.Ship.LandingSpeedMultiplier, "ship.landingSpeedMultiplier");
        rules.Ship.DropshipSpeedMultiplier = NormalizeMultiplier(rules.Ship.DropshipSpeedMultiplier, "ship.dropshipSpeedMultiplier");
        rules.Ship.ScannerDistanceMultiplier = NormalizeMultiplier(rules.Ship.ScannerDistanceMultiplier, "ship.scannerDistanceMultiplier");
        rules.Ship.BatteryCapacityMultiplier = NormalizeMultiplier(rules.Ship.BatteryCapacityMultiplier, "ship.batteryCapacityMultiplier");

        rules.Weather.ClearRewardMultiplier = NormalizeMultiplier(rules.Weather.ClearRewardMultiplier, "weather.clearRewardMultiplier");
        rules.Weather.RainyRewardMultiplier = NormalizeMultiplier(rules.Weather.RainyRewardMultiplier, "weather.rainyRewardMultiplier");
        rules.Weather.StormyRewardMultiplier = NormalizeMultiplier(rules.Weather.StormyRewardMultiplier, "weather.stormyRewardMultiplier");
        rules.Weather.FoggyRewardMultiplier = NormalizeMultiplier(rules.Weather.FoggyRewardMultiplier, "weather.foggyRewardMultiplier");
        rules.Weather.FloodedRewardMultiplier = NormalizeMultiplier(rules.Weather.FloodedRewardMultiplier, "weather.floodedRewardMultiplier");
        rules.Weather.EclipsedRewardMultiplier = NormalizeMultiplier(rules.Weather.EclipsedRewardMultiplier, "weather.eclipsedRewardMultiplier");

        foreach (var pair in rules.MoonRules)
        {
            var moonRule = pair.Value;
            if (moonRule == null)
            {
                OPLog.Warning("RuntimeRules", $"Moon rule '{pair.Key}' is null and will be ignored.");
                continue;
            }

            moonRule.RoutePriceMultiplier = NormalizeMultiplier(moonRule.RoutePriceMultiplier, $"moonRules.{pair.Key}.routePriceMultiplier");
            moonRule.ScrapValueMultiplier = NormalizeMultiplier(moonRule.ScrapValueMultiplier, $"moonRules.{pair.Key}.scrapValueMultiplier");
            moonRule.SpawnRarityMultiplier = NormalizeMultiplier(moonRule.SpawnRarityMultiplier, $"moonRules.{pair.Key}.spawnRarityMultiplier");
            moonRule.WeatherRewardMultiplier = NormalizeMultiplier(moonRule.WeatherRewardMultiplier, $"moonRules.{pair.Key}.weatherRewardMultiplier");
        }
    }

    private static float NormalizeMultiplier(float value, string path)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            OPLog.Warning("RuntimeRules", $"{path} was NaN/Infinity. Normalizing to 1.");
            return 1f;
        }

        if (value < 0f)
        {
            OPLog.Warning("RuntimeRules", $"{path}={value:0.###} is below 0. Clamping to 0.");
            return 0f;
        }

        if (value > 10f)
        {
            OPLog.Warning("RuntimeRules", $"{path}={value:0.###} is above 10. Clamping to 10.");
            return 10f;
        }

        return value;
    }
}
