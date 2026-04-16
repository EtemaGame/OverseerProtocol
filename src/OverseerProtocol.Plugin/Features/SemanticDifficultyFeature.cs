using System;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.Features;

public sealed class SemanticDifficultyFeature
{
    public float ApplyAggressionProfile(float spawnRarityMultiplier)
    {
        var profile = OPConfig.AggressionProfile.Value?.Trim() ?? "Balanced";
        var semanticMultiplier = ResolveAggressionMultiplier(profile);
        var result = spawnRarityMultiplier * semanticMultiplier;

        OPLog.Info("SemanticDifficulty", $"AggressionProfile={profile}, semanticSpawnMultiplier={semanticMultiplier:0.###}, effectiveSpawnRarityMultiplier={result:0.###}");
        return result;
    }

    private float ResolveAggressionMultiplier(string profile)
    {
        if (string.Equals(profile, "Calm", StringComparison.OrdinalIgnoreCase))
            return 0.75f;

        if (string.Equals(profile, "Balanced", StringComparison.OrdinalIgnoreCase))
            return 1f;

        if (string.Equals(profile, "Aggressive", StringComparison.OrdinalIgnoreCase))
            return 1.25f;

        if (string.Equals(profile, "Nightmare", StringComparison.OrdinalIgnoreCase))
            return 1.6f;

        OPLog.Warning("SemanticDifficulty", $"Unknown AggressionProfile '{profile}'. Falling back to Balanced.");
        return 1f;
    }
}
