using OverseerProtocol.Core.Logging;
using OverseerProtocol.Export;
using OverseerProtocol.GameAbstractions.Catalogs;
using OverseerProtocol.GameAbstractions.Resolvers;

namespace OverseerProtocol.Features;

public static class DataExportFeature
{
    public static void RunInitialExport()
    {
        OPLog.Info("Export", "Iniciando export de datos del juego...");

        // Price Resolver (Economy)
        var priceResolver = new MoonRoutePriceResolver();
        priceResolver.Initialize();

        // Items
        var itemReader = new ItemCatalogReader();
        var itemExporter = new ItemExporter();
        var items = itemReader.ReadAllItems();
        itemExporter.ExportAll(items);

        // Moons (Enriched with RoutePrice)
        var moonReader = new MoonCatalogReader();
        var moonExporter = new MoonExporter();
        var moons = moonReader.ReadAllMoons();
        
        // Enrich moons with resolved prices
        if (StartOfRound.Instance != null && StartOfRound.Instance.levels != null)
        {
            var levels = StartOfRound.Instance.levels;
            for (int i = 0; i < moons.Count; i++)
            {
                // Note: We assume moons were read in the same order as StartOfRound.Instance.levels
                // This is true in the current MoonCatalogReader implementation.
                moons[i].RoutePrice = priceResolver.GetRoutePrice(i);
            }
        }
        
        moonExporter.ExportAll(moons);

        // Moon Economy (raw TerminalNode mapping)
        var moonEconomyExporter = new MoonEconomyExporter();
        var moonEconomyProfiles = priceResolver.BuildMoonEconomyProfiles(moons);
        moonEconomyExporter.ExportAll(moonEconomyProfiles);

        // Enemies
        var enemyReader = new EnemyCatalogReader();
        var enemyExporter = new EnemyExporter();
        var enemies = enemyReader.ReadAllEnemies();
        enemyExporter.ExportAll(enemies);

        // Moon Spawn Profiles (Relational)
        var spawnReader = new MoonSpawnCatalogReader();
        var spawnExporter = new MoonSpawnExporter();
        var spawnProfiles = spawnReader.ReadAllSpawnProfiles();
        spawnExporter.ExportAll(spawnProfiles);

        OPLog.Info("Export", "Export inicial de datos completado.");
    }
}
