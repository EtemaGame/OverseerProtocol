using System;
using System.Collections.Generic;
using OverseerProtocol.Core.Validation;
using OverseerProtocol.Data.Models;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class ItemOverrideValidator
{
    public ItemOverrideValidationResult Validate(ItemOverrideCollection collection, ItemOverrideReferenceCatalog references)
    {
        var report = new ValidationReport();
        var validatedCollection = new ItemOverrideCollection();

        if (!references.HasExportedItemCatalog)
            report.Error("ITEM_CATALOG_MISSING", "Item overrides require a valid exported item catalog.");

        if (!references.HasRuntimeItemCatalog)
            report.Error("ITEM_RUNTIME_CATALOG_MISSING", "Item overrides require the runtime item catalog to be loaded.");

        if (report.HasErrors)
            return new ItemOverrideValidationResult(validatedCollection, report);

        if (collection?.Overrides == null || collection.Overrides.Count == 0)
        {
            report.Info("ITEM_OVERRIDES_EMPTY", "No item override entries were found.");
            return new ItemOverrideValidationResult(validatedCollection, report);
        }

        var observedItems = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < collection.Overrides.Count; i++)
        {
            var itemOverride = collection.Overrides[i];
            var itemPath = $"overrides[{i}]";

            if (itemOverride == null)
            {
                report.Warning("ITEM_OVERRIDE_NULL", "Found null item override entry. Skipping entry.", itemPath);
                continue;
            }

            if (string.IsNullOrWhiteSpace(itemOverride.Id))
            {
                report.Warning("ITEM_ID_EMPTY", "Found item override entry with empty Id. Skipping entry.", $"{itemPath}.id");
                continue;
            }

            if (!references.ExportedItemIds.Contains(itemOverride.Id))
            {
                report.Warning("ITEM_ID_UNKNOWN", $"Item Id '{itemOverride.Id}' does not exist in the exported item catalog. Skipping item override.", $"{itemPath}.id");
                continue;
            }

            if (!references.RuntimeItemIds.Contains(itemOverride.Id))
            {
                report.Warning("ITEM_ID_UNRESOLVED", $"Item Id '{itemOverride.Id}' does not exist in the runtime item catalog. Skipping item override.", $"{itemPath}.id");
                continue;
            }

            if (!observedItems.Add(itemOverride.Id))
            {
                report.Warning("ITEM_ID_DUPLICATE", $"Duplicate item Id '{itemOverride.Id}' found. First entry wins; duplicate skipped.", $"{itemPath}.id");
                continue;
            }

            var validatedOverride = new ItemOverrideDefinition
            {
                Id = itemOverride.Id,
                CreditsWorth = ValidateCreditsWorth(itemOverride.CreditsWorth, report, $"{itemPath}.creditsWorth", itemOverride.Id),
                Weight = ValidateWeight(itemOverride.Weight, report, $"{itemPath}.weight", itemOverride.Id)
            };

            if (!validatedOverride.CreditsWorth.HasValue && !validatedOverride.Weight.HasValue)
            {
                report.Warning("ITEM_OVERRIDE_EMPTY", $"Item override for '{itemOverride.Id}' does not define any supported fields. Skipping item override.", itemPath);
                continue;
            }

            validatedCollection.Overrides.Add(validatedOverride);
        }

        return new ItemOverrideValidationResult(validatedCollection, report);
    }

    private static int? ValidateCreditsWorth(int? creditsWorth, ValidationReport report, string path, string itemId)
    {
        if (!creditsWorth.HasValue)
            return null;

        if (creditsWorth.Value < 0)
        {
            report.Warning("ITEM_CREDITS_WORTH_CLAMPED", $"Item '{itemId}' creditsWorth {creditsWorth.Value} is below 0. Clamped to 0.", path);
            return 0;
        }

        return creditsWorth.Value;
    }

    private static float? ValidateWeight(float? weight, ValidationReport report, string path, string itemId)
    {
        if (!weight.HasValue)
            return null;

        if (float.IsNaN(weight.Value) || float.IsInfinity(weight.Value))
        {
            report.Warning("ITEM_WEIGHT_INVALID", $"Item '{itemId}' weight is not finite. Field skipped.", path);
            return null;
        }

        if (weight.Value < 0f)
        {
            report.Warning("ITEM_WEIGHT_CLAMPED", $"Item '{itemId}' weight {weight.Value:0.###} is below 0. Clamped to 0.", path);
            return 0f;
        }

        return weight.Value;
    }
}
