using OverseerProtocol.Core.Logging;
using OverseerProtocol.GameAbstractions.Overrides;

namespace OverseerProtocol.Features;

public sealed class RuntimeMultiplierFeature
{
    private readonly RuntimeMultiplierApplier _applier = new();

    public void ApplyMultipliers(float itemWeightMultiplier, float spawnRarityMultiplier, float routePriceMultiplier)
    {
        OPLog.Info("Overrides", $"Applying runtime multipliers: itemWeight={itemWeightMultiplier:0.###}, spawnRarity={spawnRarityMultiplier:0.###}, routePrice={routePriceMultiplier:0.###}");
        _applier.Apply(itemWeightMultiplier, spawnRarityMultiplier, routePriceMultiplier);
    }
}
