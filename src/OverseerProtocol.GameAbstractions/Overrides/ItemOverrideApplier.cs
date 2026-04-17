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
            OPLog.Info("Overrides", "No item tuning entries found to apply.");
            return;
        }

        // Runtime safety checks before applying user tuning.
        if (StartOfRound.Instance == null)
        {
            OPLog.Warning("Overrides", "StartOfRound.Instance is null. Aborting item tuning application.");
            return;
        }

        if (StartOfRound.Instance.allItemsList == null)
        {
            OPLog.Warning("Overrides", "allItemsList is null. Aborting item tuning application.");
            return;
        }

        var items = StartOfRound.Instance.allItemsList.itemsList;
        if (items == null)
        {
            OPLog.Warning("Overrides", "itemsList is null. Aborting item tuning application.");
            return;
        }

        OPLog.Info("Overrides", $"Applying {collection.Overrides.Count} item tuning entries to catalog.");

        var appliedCount = 0;
        var skippedCount = 0;
        foreach (var overrideDef in collection.Overrides)
        {
            if (string.IsNullOrWhiteSpace(overrideDef.Id))
            {
                OPLog.Warning("Overrides", "Item tuning entry with empty id was skipped.");
                skippedCount++;
                continue;
            }

            // item.name is the stable exported ID used in user tuning files.
            var targetItem = items.Find(i => i != null && i.name == overrideDef.Id);

            if (targetItem == null)
            {
                OPLog.Warning("Overrides", $"Item with ID '{overrideDef.Id}' not found in catalog. Skipping.");
                skippedCount++;
                continue;
            }

            ApplyToItem(targetItem, overrideDef);
            appliedCount++;
        }

        OPLog.Info("Overrides", $"Successfully applied {appliedCount} item tuning entries. skipped={skippedCount}");
    }

    private void ApplyToItem(Item item, ItemOverrideDefinition def)
    {
        if (def.CreditsWorth.HasValue)
        {
            OPLog.Info("Overrides", $"Overriding {item.name}.creditsWorth: {item.creditsWorth} -> {def.CreditsWorth.Value}");
            item.creditsWorth = def.CreditsWorth.Value;
        }

        if (def.Weight.HasValue)
        {
            OPLog.Info("Overrides", $"Overriding {item.name}.weight: {item.weight:0.###} -> {def.Weight.Value:0.###}");
            item.weight = def.Weight.Value;
        }

        if (!def.CreditsWorth.HasValue && !def.Weight.HasValue)
            OPLog.Warning("Overrides", $"Item tuning entry for {item.name} had no active runtime fields.");
    }
}
