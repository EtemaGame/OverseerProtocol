using System.Collections.Generic;
using System.Linq;
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
            ApplyStoreFields(targetItem, overrideDef);
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

        if (def.MinValue.HasValue)
        {
            OPLog.Info("Overrides", $"Overriding {item.name}.minValue: {item.minValue} -> {def.MinValue.Value}");
            item.minValue = def.MinValue.Value;
        }

        if (def.MaxValue.HasValue)
        {
            OPLog.Info("Overrides", $"Overriding {item.name}.maxValue: {item.maxValue} -> {def.MaxValue.Value}");
            item.maxValue = def.MaxValue.Value;
        }

        if (!def.CreditsWorth.HasValue && !def.Weight.HasValue && !def.MinValue.HasValue && !def.MaxValue.HasValue)
            OPLog.Info("Overrides", $"Item tuning entry for {item.name} had no direct value/weight/range fields.");
    }

    private static void ApplyStoreFields(Item item, ItemOverrideDefinition def)
    {
        if (def.StorePrice.HasValue && !def.CreditsWorth.HasValue)
        {
            OPLog.Info("Overrides", $"Applying {item.name}.storePrice through creditsWorth: {item.creditsWorth} -> {def.StorePrice.Value}");
            item.creditsWorth = def.StorePrice.Value;
        }

        if (def.AddToStore != true)
            return;

        var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
        if (terminal == null)
        {
            OPLog.Warning("Overrides", $"Cannot add '{item.name}' to store because Terminal was not found.");
            return;
        }

        if (terminal.buyableItemsList == null)
        {
            terminal.buyableItemsList = new[] { item };
            EnsureSalesArrayLength(terminal);
            OPLog.Info("Overrides", $"Added '{item.name}' to empty Terminal.buyableItemsList.");
            return;
        }

        if (terminal.buyableItemsList.Any(existing => existing != null && existing.name == item.name))
        {
            OPLog.Info("Overrides", $"Item '{item.name}' is already present in Terminal.buyableItemsList.");
            EnsureSalesArrayLength(terminal);
            return;
        }

        var list = terminal.buyableItemsList.ToList();
        list.Add(item);
        terminal.buyableItemsList = list.ToArray();
        EnsureSalesArrayLength(terminal);
        OPLog.Info("Overrides", $"Added '{item.name}' to Terminal.buyableItemsList. storeCount={terminal.buyableItemsList.Length}");
    }

    private static void EnsureSalesArrayLength(Terminal terminal)
    {
        var count = terminal.buyableItemsList?.Length ?? 0;
        if (count <= 0)
            return;

        if (terminal.itemSalesPercentages != null && terminal.itemSalesPercentages.Length >= count)
            return;

        var sales = new int[count];
        if (terminal.itemSalesPercentages != null)
        {
            for (var i = 0; i < terminal.itemSalesPercentages.Length && i < sales.Length; i++)
                sales[i] = terminal.itemSalesPercentages[i];
        }

        terminal.itemSalesPercentages = sales;
    }
}
