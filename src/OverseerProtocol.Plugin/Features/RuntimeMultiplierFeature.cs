using OverseerProtocol.Core.Logging;
using OverseerProtocol.GameAbstractions.Overrides;

namespace OverseerProtocol.Features;

public sealed class RuntimeMultiplierFeature
{
    private readonly RuntimeMultiplierApplier _applier = new();

    public void ApplyMultipliers(float itemWeightMultiplier, float spawnRarityMultiplier)
    {
        OPLog.Info("Overrides", $"Applying runtime multipliers: itemWeight={itemWeightMultiplier:0.###}, spawnRarity={spawnRarityMultiplier:0.###}");
        _applier.Apply(itemWeightMultiplier, spawnRarityMultiplier);
    }
}
