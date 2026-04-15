using System.Collections.Generic;
using UnityEngine;
using OverseerProtocol.Data.Models.Items;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.GameAbstractions.Catalogs;

public class ItemCatalogReader
{
    public List<ItemDefinition> ReadAllItems()
    {
        var definitions = new List<ItemDefinition>();

        if (StartOfRound.Instance == null)
        {
            OPLog.Warning("ItemCatalogReader: StartOfRound.Instance is null. Cannot read item catalog yet.");
            return definitions;
        }

        if (StartOfRound.Instance.allItemsList == null)
        {
            OPLog.Warning("ItemCatalogReader: allItemsList is null on StartOfRound.Instance.");
            return definitions;
        }

        var items = StartOfRound.Instance.allItemsList.itemsList;

        if (items == null)
        {
            OPLog.Warning("ItemCatalogReader: itemsList is null in allItemsList.");
            return definitions;
        }

        OPLog.Info($"ItemCatalogReader: Found {items.Count} items in StartOfRound allItemsList.");

        foreach (var item in items)
        {
            if (item == null) continue;

            var def = new ItemDefinition
            {
                Id = item.name,
                Name = item.itemName,
                CreditsWorth = item.creditsWorth,
                Weight = item.weight,
                IsScrap = item.isScrap,
                IsConductiveMetal = item.isConductiveMetal,
                RequiresBattery = item.requiresBattery,
                SpawnPrefabName = item.spawnPrefab != null ? item.spawnPrefab.name : null,
                MaxStack = 1
            };

            definitions.Add(def);
        }

        return definitions;
    }
}
