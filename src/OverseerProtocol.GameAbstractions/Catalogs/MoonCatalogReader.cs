using System.Collections.Generic;
using OverseerProtocol.Data.Models.Moons;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.GameAbstractions.Catalogs;

public class MoonCatalogReader
{
    public List<MoonDefinition> ReadAllMoons()
    {
        var definitions = new List<MoonDefinition>();

        if (StartOfRound.Instance == null)
        {
            OPLog.Warning("Moons", "StartOfRound.Instance is null. Cannot read moon catalog yet.");
            return definitions;
        }

        var levels = StartOfRound.Instance.levels;

        if (levels == null || levels.Length == 0)
        {
            OPLog.Warning("Moons", "StartOfRound.Instance.levels is null or empty. Aborting moon export to prevent false positives.");
            return definitions;
        }

        OPLog.Info("Moons", $"Found {levels.Length} levels in StartOfRound catalog.");

        for (var levelIndex = 0; levelIndex < levels.Length; levelIndex++)
        {
            var level = levels[levelIndex];
            if (level == null) continue;

            // Mapping verified against Assembly-CSharp.dll
            var def = new MoonDefinition
            {
                Id = level.name, // ScriptableObject name (e.g. ExperimentationLevel)
                InternalName = level.PlanetName, // e.g. "41 Experimentation"
                DisplayName = level.PlanetName, 
                LevelIndex = levelIndex,
                RiskLevel = ParseRiskLevel(level.riskLevel)
            };

            definitions.Add(def);
        }

        return definitions;
    }

    private int ParseRiskLevel(string risk)
    {
        // Simple heuristic for V1, can be refined later
        if (string.IsNullOrEmpty(risk)) return 0;
        if (risk.Contains("S")) return 5;
        if (risk.Contains("A")) return 4;
        if (risk.Contains("B")) return 3;
        if (risk.Contains("C")) return 2;
        if (risk.Contains("D")) return 1;
        return 0;
    }
}
