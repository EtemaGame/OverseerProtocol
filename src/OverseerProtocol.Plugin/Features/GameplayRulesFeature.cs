using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models.Rules;
using OverseerProtocol.GameAbstractions.Overrides;

namespace OverseerProtocol.Features;

public sealed class GameplayRulesFeature
{
    private readonly GameplayRouteRulesApplier _applier = new();

    public GameplayRouteRulesDefinition LoadOrCreate()
    {
        var rules = new RuntimeTuningConfig(OPConfig.ConfigFile).BuildGameplayRouteRules();
        Normalize(rules);
        OPLog.Info("Gameplay", $"Loaded gameplay route rules from .cfg: travelDiscount={rules.Economy.TravelDiscountMultiplier:0.###}, moonRouteMultipliers={rules.MoonRules.Count}");
        return rules;
    }

    public void Apply(GameplayRouteRulesDefinition rules)
    {
        OPLog.Info("Gameplay", "Applying route multipliers from [Gameplay] and [Moons.<MoonId>].");
        _applier.Apply(rules);
    }

    private static void Normalize(GameplayRouteRulesDefinition rules)
    {
        if (rules.SchemaVersion <= 0)
        {
            OPLog.Info("Gameplay", $"Normalizing schemaVersion {rules.SchemaVersion} -> 1");
            rules.SchemaVersion = 1;
        }

        rules.Economy ??= new EconomyRulesDefinition();
        rules.Ship ??= new ShipRulesDefinition();
        rules.Weather ??= new WeatherRulesDefinition();
        rules.MoonRules ??= new System.Collections.Generic.Dictionary<string, MoonGameplayRouteRuleDefinition>();

        rules.Economy.TravelDiscountMultiplier = NormalizeMultiplier(rules.Economy.TravelDiscountMultiplier, "Gameplay.TravelDiscountMultiplier");

        foreach (var pair in rules.MoonRules)
        {
            var moonRule = pair.Value;
            if (moonRule == null)
            {
                OPLog.Warning("Gameplay", $"Moon route multiplier '{pair.Key}' is null and will be ignored.");
                continue;
            }

            moonRule.RoutePriceMultiplier = NormalizeMultiplier(moonRule.RoutePriceMultiplier, $"Moons.{pair.Key}.RouteMultiplier");
        }
    }

    private static float NormalizeMultiplier(float value, string path)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            OPLog.Warning("Gameplay", $"{path} was NaN/Infinity. Normalizing to 1.");
            return 1f;
        }

        if (value < 0f)
        {
            OPLog.Warning("Gameplay", $"{path}={value:0.###} is below 0. Clamping to 0.");
            return 0f;
        }

        if (value > 10f)
        {
            OPLog.Warning("Gameplay", $"{path}={value:0.###} is above 10. Clamping to 10.");
            return 10f;
        }

        return value;
    }
}
