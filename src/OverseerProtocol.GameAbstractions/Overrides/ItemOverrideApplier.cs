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

        if (def.IsScrap.HasValue)
        {
            OPLog.Info("Overrides", $"Overriding {item.name}.isScrap: {item.isScrap} -> {def.IsScrap.Value}");
            item.isScrap = def.IsScrap.Value;
        }

        if (def.RequiresBattery.HasValue)
        {
            OPLog.Info("Overrides", $"Overriding {item.name}.requiresBattery: {item.requiresBattery} -> {def.RequiresBattery.Value}");
            item.requiresBattery = def.RequiresBattery.Value;
        }

        if (def.IsConductiveMetal.HasValue)
        {
            OPLog.Info("Overrides", $"Overriding {item.name}.isConductiveMetal: {item.isConductiveMetal} -> {def.IsConductiveMetal.Value}");
            item.isConductiveMetal = def.IsConductiveMetal.Value;
        }

        if (def.TwoHanded.HasValue)
        {
            OPLog.Info("Overrides", $"Overriding {item.name}.twoHanded: {item.twoHanded} -> {def.TwoHanded.Value}");
            item.twoHanded = def.TwoHanded.Value;
        }

        if (!def.CreditsWorth.HasValue && !def.Weight.HasValue && !def.MinValue.HasValue && !def.MaxValue.HasValue && !def.IsScrap.HasValue && !def.RequiresBattery.HasValue && !def.IsConductiveMetal.HasValue && !def.TwoHanded.HasValue)
            OPLog.Info("Overrides", $"Item tuning entry for {item.name} had no direct value/weight/range/flag fields.");
    }

    private static void ApplyStoreFields(Item item, ItemOverrideDefinition def)
    {
        if (def.StorePrice.HasValue && !def.CreditsWorth.HasValue)
        {
            OPLog.Info("Overrides", $"Applying {item.name}.storePrice through creditsWorth: {item.creditsWorth} -> {def.StorePrice.Value}");
            item.creditsWorth = def.StorePrice.Value;
        }

        if (!def.AddToStore.HasValue)
            return;

        var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
        if (terminal == null)
        {
            OPLog.Warning("Overrides", $"Cannot update store membership for '{item.name}' because Terminal was not found.");
            return;
        }

        if (def.AddToStore.Value == false)
        {
            RemoveFromStore(terminal, item);
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

    private static void RemoveFromStore(Terminal terminal, Item item)
    {
        if (terminal.buyableItemsList == null || terminal.buyableItemsList.Length == 0)
            return;

        var before = terminal.buyableItemsList.Length;
        terminal.buyableItemsList = terminal.buyableItemsList
            .Where(existing => existing == null || existing.name != item.name)
            .ToArray();

        if (terminal.buyableItemsList.Length == before)
        {
            OPLog.Info("Overrides", $"Item '{item.name}' is not present in Terminal.buyableItemsList.");
            return;
        }

        ShrinkSalesArrayLength(terminal);
        OPLog.Info("Overrides", $"Removed '{item.name}' from Terminal.buyableItemsList. storeCount={terminal.buyableItemsList.Length}");
    }

    private static void ShrinkSalesArrayLength(Terminal terminal)
    {
        var count = terminal.buyableItemsList?.Length ?? 0;
        if (terminal.itemSalesPercentages == null || terminal.itemSalesPercentages.Length == count)
            return;

        var sales = new int[count];
        for (var i = 0; i < terminal.itemSalesPercentages.Length && i < sales.Length; i++)
            sales[i] = terminal.itemSalesPercentages[i];

        terminal.itemSalesPercentages = sales;
    }
}
