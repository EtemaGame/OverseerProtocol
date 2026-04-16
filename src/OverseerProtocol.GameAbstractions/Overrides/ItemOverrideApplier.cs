using System.Collections.Generic;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models;
using UnityEngine;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class ItemOverrideApplier
{
    public void Apply(ItemOverrideCollection collection)
    {
        if (collection?.Overrides == null || collection.Overrides.Count == 0)
        {
            OPLog.Info("Overrides", "No item overrides found to apply.");
            return;
        }

        // Safety checks per user request Adjustment 1
        if (StartOfRound.Instance == null)
        {
            OPLog.Warning("Overrides", "StartOfRound.Instance is null. Aborting override application.");
            return;
        }

        if (StartOfRound.Instance.allItemsList == null)
        {
            OPLog.Warning("Overrides", "allItemsList is null. Aborting override application.");
            return;
        }

        var items = StartOfRound.Instance.allItemsList.itemsList;
        if (items == null)
        {
            OPLog.Warning("Overrides", "itemsList is null. Aborting override application.");
            return;
        }

        OPLog.Info("Overrides", $"Applying {collection.Overrides.Count} overrides to catalog.");

        var appliedCount = 0;
        foreach (var overrideDef in collection.Overrides)
        {
            if (string.IsNullOrWhiteSpace(overrideDef.Id)) continue;

            // Matching Logic: Using item.name (exported ID) per user request Adjustment 2
            var targetItem = items.Find(i => i != null && i.name == overrideDef.Id);

            if (targetItem == null)
            {
                OPLog.Warning("Overrides", $"Item with ID '{overrideDef.Id}' not found in catalog. Skipping.");
                continue;
            }

            ApplyToItem(targetItem, overrideDef);
            appliedCount++;
        }

        OPLog.Info("Overrides", $"Successfully applied {appliedCount} item overrides.");
    }

    private void ApplyToItem(Item item, ItemOverrideDefinition def)
    {
        if (def.CreditsWorth.HasValue)
        {
            OPLog.Debug("Overrides", $"Overriding {item.name}.creditsWorth: {item.creditsWorth} -> {def.CreditsWorth.Value}");
            item.creditsWorth = def.CreditsWorth.Value;
        }

        if (def.Weight.HasValue)
        {
            OPLog.Debug("Overrides", $"Overriding {item.name}.weight: {item.weight} -> {def.Weight.Value}");
            item.weight = def.Weight.Value;
        }
    }
}
