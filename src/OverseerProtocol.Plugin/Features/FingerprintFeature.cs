using System.Collections.Generic;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Security;

namespace OverseerProtocol.Features;

public sealed class FingerprintFeature
{
    public RuntimeFingerprints ComputeCurrent()
    {
        var presetName = OPConfig.ActivePresetName;

        var presetFiles = new List<string>
        {
            OPPaths.GetPresetManifestPath(presetName),
            OPPaths.GetItemOverridePath(presetName),
            OPPaths.GetSpawnOverridePath(presetName),
            OPPaths.GetMoonOverridePath(presetName),
            OPPaths.GetPresetLobbyRulesPath(presetName),
            OPPaths.GetPresetRuntimeRulesPath(presetName)
        };

        var configText =
            "preset=" + presetName + "\n" +
            "itemOverrides=" + OPConfig.EnableItemOverrides.Value + "\n" +
            "spawnOverrides=" + OPConfig.EnableSpawnOverrides.Value + "\n" +
            "moonOverrides=" + OPConfig.EnableMoonOverrides.Value + "\n" +
            "runtimeMultipliers=" + OPConfig.EnableRuntimeMultipliers.Value + "\n" +
            "strictValidation=" + OPConfig.StrictValidation.Value + "\n" +
            "dryRunOverrides=" + OPConfig.DryRunOverrides.Value + "\n" +
            "itemWeightMultiplier=" + OPConfig.ItemWeightMultiplier.Value.ToString("R") + "\n" +
            "spawnRarityMultiplier=" + OPConfig.SpawnRarityMultiplier.Value.ToString("R") + "\n" +
            "routePriceMultiplier=" + OPConfig.RoutePriceMultiplier.Value.ToString("R") + "\n" +
            "aggressionProfile=" + OPConfig.AggressionProfile.Value + "\n";

        return new RuntimeFingerprints
        {
            ActivePreset = presetName,
            PresetFingerprint = FingerprintUtility.ComputeCombinedFileHash(presetFiles),
            ConfigFingerprint = FingerprintUtility.ComputeTextHash(configText)
        };
    }
}

public sealed class RuntimeFingerprints
{
    public string ActivePreset { get; set; } = "default";
    public string PresetFingerprint { get; set; } = "";
    public string ConfigFingerprint { get; set; } = "";
}
