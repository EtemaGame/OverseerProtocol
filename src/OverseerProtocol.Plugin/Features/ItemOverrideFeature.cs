using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models;
using OverseerProtocol.GameAbstractions.Overrides;

namespace OverseerProtocol.Features;

public sealed class ItemOverrideFeature
{
    private readonly ItemOverrideApplier _applier;

    public ItemOverrideFeature()
    {
        _applier = new ItemOverrideApplier();
    }

    public void ApplyOverrides()
    {
        var path = OPPaths.ItemOverridePath;
        
        OPLog.Info("Overrides", $"Loading item overrides from: {path}");
        
        var collection = JsonFileReader.Read<ItemOverrideCollection>(path);
        
        if (collection == null)
        {
            OPLog.Info("Overrides", "No valid items.override.json found or file is empty.");
            return;
        }

        _applier.Apply(collection);
    }
}
