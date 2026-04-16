using System.Collections.Generic;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models;
using OverseerProtocol.Data.Models.Items;
using OverseerProtocol.GameAbstractions.Overrides;

namespace OverseerProtocol.Features;

public sealed class ItemOverrideFeature
{
    private readonly ItemOverrideValidator _validator;
    private readonly ItemOverrideApplier _applier;

    public ItemOverrideFeature()
    {
        _validator = new ItemOverrideValidator();
        _applier = new ItemOverrideApplier();
    }

    public void ApplyOverrides(string presetName)
    {
        var path = OPPaths.GetItemOverridePath(presetName);
        
        OPLog.Info("Overrides", $"Loading item overrides from: {path}");
        
        var collection = JsonFileReader.Read<ItemOverrideCollection>(path);
        
        if (collection == null)
        {
            OPLog.Info("Overrides", "No valid items.override.json found or file is empty.");
            return;
        }

        var references = LoadReferenceCatalog();
        var validationResult = _validator.Validate(collection, references);
        validationResult.Report.WriteToLog("Validation");

        var abortOnWarnings = OPConfig.StrictValidation.Value || OPConfig.AbortOnInvalidOverrideBlock.Value;
        if (!validationResult.CanApplyWithStrictMode(abortOnWarnings))
        {
            var reason = abortOnWarnings && validationResult.Report.WarningCount > 0
                ? "strict override validation is enabled and warnings were reported"
                : "critical validation errors were reported";

            OPLog.Warning("Validation", $"Item override collection was not applied because {reason}.");
            return;
        }

        if (OPConfig.DryRunOverrides.Value)
        {
            OPLog.Info("Overrides", $"Dry-run enabled. {validationResult.Collection.Overrides.Count} item overrides validated; no runtime item mutations applied.");
            return;
        }

        _applier.Apply(validationResult.Collection);
    }

    private ItemOverrideReferenceCatalog LoadReferenceCatalog()
    {
        var catalog = new ItemOverrideReferenceCatalog();

        var items = JsonFileReader.Read<List<ItemDefinition>>(OPPaths.ItemExportPath);
        if (items == null || items.Count == 0)
        {
            OPLog.Warning("Validation", $"No exported item catalog found at {OPPaths.ItemExportPath}.");
        }
        else
        {
            foreach (var item in items)
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.Id))
                    catalog.ExportedItemIds.Add(item.Id);
            }

            OPLog.Info("Validation", $"Loaded {catalog.ExportedItemIds.Count} exported item IDs for item override validation.");
        }

        if (StartOfRound.Instance?.allItemsList?.itemsList == null)
        {
            OPLog.Warning("Validation", "Runtime item catalog is not available.");
        }
        else
        {
            foreach (var item in StartOfRound.Instance.allItemsList.itemsList)
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.name))
                    catalog.RuntimeItemIds.Add(item.name);
            }

            OPLog.Info("Validation", $"Loaded {catalog.RuntimeItemIds.Count} runtime item IDs for item override validation.");
        }

        return catalog;
    }
}
