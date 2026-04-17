using System.Collections.Generic;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models;
using OverseerProtocol.Data.Models.Items;
using OverseerProtocol.Data.Models.UserConfig;
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
        var path = OPPaths.ItemsConfigPath;
        
        OPLog.Info("Overrides", $"Loading item tuning from: {path}");
        
        var userConfig = JsonFileReader.Read<UserItemConfigFile>(path);
        
        if (userConfig == null)
        {
            OPLog.Info("Overrides", "No valid items.json found or file is empty.");
            return;
        }

        var collection = BuildOverrideCollection(userConfig);
        OPLog.Info("Overrides", $"Item tuning file loaded: schemaVersion={userConfig.SchemaVersion}, itemCount={userConfig.Items?.Count ?? 0}, activeTuningCount={collection.Overrides.Count}");

        var references = LoadReferenceCatalog();
        OPLog.Info("Validation", $"Item validation references: exportedIds={references.ExportedItemIds.Count}, runtimeIds={references.RuntimeItemIds.Count}");
        var validationResult = _validator.Validate(collection, references);
        validationResult.Report.WriteToLog("Validation");

        var abortOnWarnings = OPConfig.StrictValidation.Value || OPConfig.AbortOnInvalidOverrideBlock.Value;
        OPLog.Info("Validation", $"Item validation policy: strict={OPConfig.StrictValidation.Value}, abortOnInvalidBlock={OPConfig.AbortOnInvalidOverrideBlock.Value}, abortOnWarnings={abortOnWarnings}, canApply={validationResult.CanApplyWithStrictMode(abortOnWarnings)}");
        if (!validationResult.CanApplyWithStrictMode(abortOnWarnings))
        {
            var reason = abortOnWarnings && validationResult.Report.WarningCount > 0
                ? "strict tuning validation is enabled and warnings were reported"
                : "critical validation errors were reported";

            OPLog.Warning("Validation", $"Item tuning was not applied because {reason}.");
            return;
        }

        if (OPConfig.DryRunOverrides.Value)
        {
            OPLog.Info("Overrides", $"Dry-run enabled. {validationResult.Collection.Overrides.Count} item tuning entries validated; no runtime item mutations applied.");
            return;
        }

        _applier.Apply(validationResult.Collection);
    }

    private static ItemOverrideCollection BuildOverrideCollection(UserItemConfigFile userConfig)
    {
        var collection = new ItemOverrideCollection
        {
            SchemaVersion = userConfig.SchemaVersion
        };

        if (userConfig.Items == null)
            return collection;

        foreach (var item in userConfig.Items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
                continue;

            if (!item.Enabled)
            {
                OPLog.Info("Overrides", $"Item tuning disabled for '{item.Id}'. Skipping.");
                continue;
            }

            if (item.MissingFromRuntime)
            {
                OPLog.Warning("Overrides", $"Item tuning '{item.Id}' is marked missingFromRuntime. Skipping.");
                continue;
            }

            LogReservedItemFields(item);

            if (item.Override == null ||
                (!item.Override.CreditsWorth.HasValue && !item.Override.Weight.HasValue))
            {
                OPLog.Info("Overrides", $"Item '{item.Id}' has no active supported tuning fields.");
                continue;
            }

            collection.Overrides.Add(new ItemOverrideDefinition
            {
                Id = item.Id,
                CreditsWorth = item.Override.CreditsWorth,
                Weight = item.Override.Weight
            });
        }

        return collection;
    }

    private static void LogReservedItemFields(UserItemConfig item)
    {
        if (item.Store?.AddToStore != null ||
            item.Store?.StorePrice != null ||
            item.Store?.MaxStoreStock != null)
        {
            OPLog.Warning("Overrides", $"Item '{item.Id}' store fields are reserved until shop item list hooks are verified.");
        }

        if (item.Battery?.RequiresBattery != null ||
            item.Battery?.BatteryUsageMultiplier != null ||
            item.Battery?.BatteryCapacityMultiplier != null)
        {
            OPLog.Warning("Overrides", $"Item '{item.Id}' battery fields are reserved until item battery hooks are verified.");
        }

        if (item.Spawn?.AllowAsScrap != null ||
            item.Spawn?.MinValue != null ||
            item.Spawn?.MaxValue != null)
        {
            OPLog.Warning("Overrides", $"Item '{item.Id}' scrap spawn/value fields are reserved until scrap item hooks are verified.");
        }
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

            OPLog.Info("Validation", $"Loaded {catalog.ExportedItemIds.Count} exported item IDs for item tuning validation.");
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

            OPLog.Info("Validation", $"Loaded {catalog.RuntimeItemIds.Count} runtime item IDs for item tuning validation.");
        }

        return catalog;
    }
}
