using System;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Rules;

namespace OverseerProtocol.Features;

public sealed class RuntimeRulesFeature
{
    public RuntimeRulesDefinition LoadOrCreate()
    {
        var path = OPPaths.GetPresetRuntimeRulesPath(OPConfig.ActivePresetName);
        var rules = JsonFileReader.Read<RuntimeRulesDefinition>(path);

        if (rules == null)
        {
            rules = new RuntimeRulesDefinition();
            JsonFileWriter.Write(path, rules);
            OPLog.Info("RuntimeRules", $"Created runtime rules file at {path}");
        }

        Normalize(rules);
        OPLog.Info("RuntimeRules", $"Loaded runtime rules from {path}: quota={rules.Economy.QuotaMultiplier:0.###}, deadline={rules.Economy.DeadlineMultiplier:0.###}, moonRules={rules.MoonRules.Count}");
        return rules;
    }

    private static void Normalize(RuntimeRulesDefinition rules)
    {
        if (rules.SchemaVersion <= 0)
            rules.SchemaVersion = 1;

        rules.Economy ??= new EconomyRulesDefinition();
        rules.Ship ??= new ShipRulesDefinition();
        rules.Weather ??= new WeatherRulesDefinition();
        rules.MoonRules ??= new System.Collections.Generic.Dictionary<string, MoonRuntimeRuleDefinition>();

        rules.Economy.QuotaMultiplier = NormalizeMultiplier(rules.Economy.QuotaMultiplier);
        rules.Economy.DeadlineMultiplier = NormalizeMultiplier(rules.Economy.DeadlineMultiplier);
        rules.Economy.TravelDiscountMultiplier = NormalizeMultiplier(rules.Economy.TravelDiscountMultiplier);
        rules.Economy.ScrapValueMultiplier = NormalizeMultiplier(rules.Economy.ScrapValueMultiplier);

        rules.Ship.LandingSpeedMultiplier = NormalizeMultiplier(rules.Ship.LandingSpeedMultiplier);
        rules.Ship.DropshipSpeedMultiplier = NormalizeMultiplier(rules.Ship.DropshipSpeedMultiplier);
        rules.Ship.ScannerDistanceMultiplier = NormalizeMultiplier(rules.Ship.ScannerDistanceMultiplier);
        rules.Ship.BatteryCapacityMultiplier = NormalizeMultiplier(rules.Ship.BatteryCapacityMultiplier);

        rules.Weather.ClearRewardMultiplier = NormalizeMultiplier(rules.Weather.ClearRewardMultiplier);
        rules.Weather.RainyRewardMultiplier = NormalizeMultiplier(rules.Weather.RainyRewardMultiplier);
        rules.Weather.StormyRewardMultiplier = NormalizeMultiplier(rules.Weather.StormyRewardMultiplier);
        rules.Weather.FoggyRewardMultiplier = NormalizeMultiplier(rules.Weather.FoggyRewardMultiplier);
        rules.Weather.FloodedRewardMultiplier = NormalizeMultiplier(rules.Weather.FloodedRewardMultiplier);
        rules.Weather.EclipsedRewardMultiplier = NormalizeMultiplier(rules.Weather.EclipsedRewardMultiplier);

        foreach (var moonRule in rules.MoonRules.Values)
        {
            if (moonRule == null)
                continue;

            moonRule.RoutePriceMultiplier = NormalizeMultiplier(moonRule.RoutePriceMultiplier);
            moonRule.ScrapValueMultiplier = NormalizeMultiplier(moonRule.ScrapValueMultiplier);
            moonRule.SpawnRarityMultiplier = NormalizeMultiplier(moonRule.SpawnRarityMultiplier);
            moonRule.WeatherRewardMultiplier = NormalizeMultiplier(moonRule.WeatherRewardMultiplier);
        }
    }

    private static float NormalizeMultiplier(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return 1f;

        return Math.Max(0f, Math.Min(10f, value));
    }
}
