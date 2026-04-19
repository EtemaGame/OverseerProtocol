using OverseerProtocol.Core.Logging;
using OverseerProtocol.Export;
using OverseerProtocol.GameAbstractions.Catalogs;
using OverseerProtocol.GameAbstractions.Resolvers;

namespace OverseerProtocol.Features;

public static class DataExportFeature
{
    public static void RunInitialExport()
    {
        RuntimeDiagnostics.LogTestSignal("Export", "=== TEST SIGNAL: DATA EXPORT BEGIN ===");
        OPLog.Info("Export", "Iniciando export de datos del juego...");

        // Price Resolver (Economy)
        OPLog.Info("Export", "Export step 1/6: initializing moon route price resolver.");
        var priceResolver = new MoonRoutePriceResolver();
        priceResolver.Initialize();

        // Items
        OPLog.Info("Export", "Export step 2/6: reading and writing item catalog.");
        var itemReader = new ItemCatalogReader();
        var itemExporter = new ItemExporter();
        var items = itemReader.ReadAllItems();
        OPLog.Info("Export", $"Item catalog read result: count={items.Count}");
        itemExporter.ExportAll(items);

        // Moons (Enriched with RoutePrice)
        OPLog.Info("Export", "Export step 3/6: reading and writing moon catalog.");
        var moonReader = new MoonCatalogReader();
        var moonExporter = new MoonExporter();
        var moons = moonReader.ReadAllMoons();
        OPLog.Info("Export", $"Moon catalog read result: count={moons.Count}");
        
        // Enrich moons with resolved prices
        if (StartOfRound.Instance != null && StartOfRound.Instance.levels != null)
        {
            OPLog.Info("Export", "Enriching moon export with route prices.");
            for (int i = 0; i < moons.Count; i++)
            {
                moons[i].RoutePrice = priceResolver.GetRoutePrice(moons[i].LevelIndex);
                OPLog.Info("Export", $"Moon route price export value: moon={moons[i].Id}, levelIndex={moons[i].LevelIndex}, routePrice={moons[i].RoutePrice}");
            }
        }
        else
        {
            OPLog.Warning("Export", "Cannot enrich moon export with route prices because StartOfRound levels are unavailable.");
        }
        
        moonExporter.ExportAll(moons);

        // Moon Economy (raw TerminalNode mapping)
        OPLog.Info("Export", "Export step 4/6: building and writing moon economy profiles.");
        var moonEconomyExporter = new MoonEconomyExporter();
        var moonEconomyProfiles = priceResolver.BuildMoonEconomyProfiles(moons);
        OPLog.Info("Export", $"Moon economy profile build result: count={moonEconomyProfiles.Count}");
        moonEconomyExporter.ExportAll(moonEconomyProfiles);

        // Enemies
        OPLog.Info("Export", "Export step 5/6: reading and writing enemy catalog.");
        var enemyReader = new EnemyCatalogReader();
        var enemyExporter = new EnemyExporter();
        var enemies = enemyReader.ReadAllEnemies();
        OPLog.Info("Export", $"Enemy catalog read result: count={enemies.Count}");
        enemyExporter.ExportAll(enemies);

        // Moon Spawn Profiles (Relational)
        OPLog.Info("Export", "Export step 6/6: reading and writing moon spawn profiles.");
        var spawnReader = new MoonSpawnCatalogReader();
        var spawnExporter = new MoonSpawnExporter();
        var spawnProfiles = spawnReader.ReadAllSpawnProfiles();
        OPLog.Info("Export", $"Moon spawn profile read result: count={spawnProfiles.Count}");
        spawnExporter.ExportAll(spawnProfiles);

        OPLog.Info("Export", "Export inicial de datos completado.");
        RuntimeDiagnostics.LogTestSignal("Export", "=== TEST SIGNAL: DATA EXPORT END ===");
    }
}
