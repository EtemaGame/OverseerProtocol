using System.Linq;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Security;

namespace OverseerProtocol.Features;

public sealed class FingerprintFeature
{
    public RuntimeFingerprints ComputeCurrent()
    {
        var presetName = OPConfig.ActivePresetName;
        var runtimeConfig = new RuntimeTuningConfig(OPConfig.ConfigFile);
        var itemOverrides = runtimeConfig.BuildItemOverrides();
        var moonOverrides = runtimeConfig.BuildMoonOverrides();
        var spawnOverrides = runtimeConfig.BuildSpawnOverrides();
        var runtimeRules = runtimeConfig.BuildRuntimeRules();

        var configText =
            "preset=" + presetName + "\n" +
            "itemTuning=" + OPConfig.EnableItemOverrides.Value + "\n" +
            "spawnTuning=" + OPConfig.EnableSpawnOverrides.Value + "\n" +
            "moonTuning=" + OPConfig.EnableMoonOverrides.Value + "\n" +
            "runtimeMultipliers=" + OPConfig.EnableRuntimeMultipliers.Value + "\n" +
            "strictValidation=" + OPConfig.StrictValidation.Value + "\n" +
            "dryRunOverrides=" + OPConfig.DryRunOverrides.Value + "\n" +
            "itemWeightMultiplier=" + OPConfig.ItemWeightMultiplier.Value.ToString("R") + "\n" +
            "spawnRarityMultiplier=" + OPConfig.SpawnRarityMultiplier.Value.ToString("R") + "\n" +
            "routePriceMultiplier=" + OPConfig.RoutePriceMultiplier.Value.ToString("R") + "\n" +
            "travelDiscountMultiplier=" + OPConfig.TravelDiscountMultiplier.Value.ToString("R") + "\n" +
            "aggressionProfile=" + OPConfig.AggressionProfile.Value + "\n" +
            "items=" + string.Join(";", itemOverrides.Overrides.OrderBy(item => item.Id).Select(item => item.Id + ":" + item.CreditsWorth + ":" + item.Weight)) + "\n" +
            "moons=" + string.Join(";", moonOverrides.Overrides.OrderBy(moon => moon.MoonId).Select(moon => moon.MoonId + ":" + moon.RoutePrice + ":" + moon.RiskLevel + ":" + moon.RiskLabel)) + "\n" +
            "spawns=" + string.Join(";", spawnOverrides.Overrides.OrderBy(spawn => spawn.MoonId).Select(FormatSpawnOverride)) + "\n" +
            "moonRules=" + string.Join(";", runtimeRules.MoonRules.OrderBy(pair => pair.Key).Select(pair => pair.Key + ":" + pair.Value.RoutePriceMultiplier.ToString("R"))) + "\n";

        OPLog.Info("Fingerprint", "Fingerprint config input:\n" + configText);

        var result = new RuntimeFingerprints
        {
            ActivePreset = presetName,
            PresetFingerprint = FingerprintUtility.ComputeTextHash("builtin-preset=" + presetName),
            ConfigFingerprint = FingerprintUtility.ComputeTextHash(configText)
        };

        OPLog.Info("Fingerprint", $"Fingerprint result: preset={result.ActivePreset}, presetHash={result.PresetFingerprint}, configHash={result.ConfigFingerprint}");
        return result;
    }

    private static string FormatSpawnOverride(OverseerProtocol.Data.Models.MoonSpawnOverride spawn) =>
        spawn.MoonId + ":" +
        FormatPool(spawn.InsideEnemies) + ":" +
        FormatPool(spawn.OutsideEnemies) + ":" +
        FormatPool(spawn.DaytimeEnemies);

    private static string FormatPool(System.Collections.Generic.IEnumerable<OverseerProtocol.Data.Models.Spawns.SpawnEntry>? entries) =>
        entries == null
            ? "keep"
            : string.Join(",", entries.Select(entry => entry.EnemyId + "=" + entry.Rarity));
}

public sealed class RuntimeFingerprints
{
    public string ActivePreset { get; set; } = "default";
    public string PresetFingerprint { get; set; } = "";
    public string ConfigFingerprint { get; set; } = "";
}
