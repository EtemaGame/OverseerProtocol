using System.Collections.Generic;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Perks;

namespace OverseerProtocol.Features;

public sealed class PerkCatalogFeature
{
    public PerkCatalogDefinition LoadOrCreate()
    {
        var catalog = JsonFileReader.Read<PerkCatalogDefinition>(OPPaths.PerkCatalogPath);
        if (catalog != null)
        {
            var normalized = Normalize(catalog);
            OPLog.Info(
                "Perks",
                $"Loaded perk catalog from {OPPaths.PerkCatalogPath}: schemaVersion={normalized.SchemaVersion}, playerPerks={normalized.PlayerPerks.Count}, shipPerks={normalized.ShipPerks.Count}");
            return normalized;
        }

        catalog = CreateDefaultCatalog();
        JsonFileWriter.Write(OPPaths.PerkCatalogPath, catalog);
        OPLog.Info("Perks", $"Created perk catalog at {OPPaths.PerkCatalogPath}");
        OPLog.Info("Perks", $"Default perk catalog seeded: playerPerks={catalog.PlayerPerks.Count}, shipPerks={catalog.ShipPerks.Count}");
        return catalog;
    }

    private static PerkCatalogDefinition Normalize(PerkCatalogDefinition catalog)
    {
        if (catalog.SchemaVersion <= 0)
        {
            OPLog.Info("Perks", $"Normalizing perk catalog schemaVersion {catalog.SchemaVersion} -> 1");
            catalog.SchemaVersion = 1;
        }

        catalog.PlayerPerks ??= new List<PerkDefinition>();
        catalog.ShipPerks ??= new List<PerkDefinition>();

        return catalog;
    }

    private static PerkCatalogDefinition CreateDefaultCatalog() =>
        new PerkCatalogDefinition
        {
            PlayerPerks =
            {
                CreateMultiplierPerk("player-sprint", "Sprint Conditioning", "Move faster while sprinting.", 5, "sprintSpeedMultiplier", 0.05f),
                CreateMultiplierPerk("player-stamina", "Endurance Training", "Use stamina more efficiently.", 5, "staminaEfficiencyMultiplier", 0.06f),
                CreateMultiplierPerk("player-carry", "Load Bearing", "Handle carried weight more effectively.", 5, "carryWeightHandlingMultiplier", 0.05f),
                CreateMultiplierPerk("player-climb", "Climber", "Climb ladders faster.", 5, "climbSpeedMultiplier", 0.05f),
                CreateMultiplierPerk("player-resistance", "Thick Skin", "Reduce incoming damage slightly.", 5, "damageResistanceMultiplier", 0.04f)
            },
            ShipPerks =
            {
                CreateMultiplierPerk("ship-scanner", "Long Scanner", "Improve ship scanner range.", 5, "scannerDistanceMultiplier", 0.08f),
                CreateMultiplierPerk("ship-battery", "Battery Banks", "Improve battery capacity.", 5, "batteryCapacityMultiplier", 0.08f),
                CreateMultiplierPerk("ship-travel-discount", "Route Negotiator", "Reduce route costs.", 5, "routePriceDiscountMultiplier", -0.04f),
                CreateMultiplierPerk("ship-dropship", "Dropship Priority", "Improve dropship timing.", 5, "dropshipSpeedMultiplier", 0.06f),
                CreateMultiplierPerk("ship-deadline", "Quota Buffer", "Improve deadline pressure.", 5, "deadlineMultiplier", 0.04f)
            }
        };

    private static PerkDefinition CreateMultiplierPerk(string id, string displayName, string description, int maxRank, string stat, float perRank)
    {
        var perk = new PerkDefinition
        {
            Id = id,
            DisplayName = displayName,
            Description = description,
            MaxRank = maxRank
        };

        for (var rank = 1; rank <= maxRank; rank++)
        {
            perk.RankCosts.Add(rank);
            perk.Effects.Add(new PerkRankEffect
            {
                Rank = rank,
                Stat = stat,
                Multiplier = 1f + perRank * rank
            });
        }

        return perk;
    }
}
