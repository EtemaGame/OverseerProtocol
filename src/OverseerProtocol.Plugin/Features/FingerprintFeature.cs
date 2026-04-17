using System.Collections.Generic;
using System.IO;
using System.Linq;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
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
            OPPaths.ItemsConfigPath,
            OPPaths.GetPresetLobbyRulesPath(presetName),
            OPPaths.GetPresetRuntimeRulesPath(presetName)
        };

        if (Directory.Exists(OPPaths.MoonConfigRoot))
            presetFiles.AddRange(Directory.GetFiles(OPPaths.MoonConfigRoot, "*.json").OrderBy(path => path, System.StringComparer.Ordinal));

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
            "aggressionProfile=" + OPConfig.AggressionProfile.Value + "\n";

        foreach (var path in presetFiles)
            OPLog.Info("Fingerprint", $"Fingerprint input file: {path}");

        OPLog.Info("Fingerprint", "Fingerprint config input:\n" + configText);

        var result = new RuntimeFingerprints
        {
            ActivePreset = presetName,
            PresetFingerprint = FingerprintUtility.ComputeCombinedFileHash(presetFiles),
            ConfigFingerprint = FingerprintUtility.ComputeTextHash(configText)
        };

        OPLog.Info("Fingerprint", $"Fingerprint result: preset={result.ActivePreset}, presetHash={result.PresetFingerprint}, configHash={result.ConfigFingerprint}");
        return result;
    }
}

public sealed class RuntimeFingerprints
{
    public string ActivePreset { get; set; } = "default";
    public string PresetFingerprint { get; set; } = "";
    public string ConfigFingerprint { get; set; } = "";
}
