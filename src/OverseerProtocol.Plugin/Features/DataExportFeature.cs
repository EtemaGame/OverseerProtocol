using OverseerProtocol.Core.Logging;
using OverseerProtocol.Export;
using OverseerProtocol.GameAbstractions.Catalogs;

namespace OverseerProtocol.Features;

public static class DataExportFeature
{
    public static void RunInitialExport()
    {
        OPLog.Info("Export", "Iniciando export de datos del juego...");

        // Items
        var itemReader = new ItemCatalogReader();
        var itemExporter = new ItemExporter();
        var items = itemReader.ReadAllItems();
        itemExporter.ExportAll(items);

        // Moons
        var moonReader = new MoonCatalogReader();
        var moonExporter = new MoonExporter();
        var moons = moonReader.ReadAllMoons();
        moonExporter.ExportAll(moons);

        // Enemies
        var enemyReader = new EnemyCatalogReader();
        var enemyExporter = new EnemyExporter();
        var enemies = enemyReader.ReadAllEnemies();
        enemyExporter.ExportAll(enemies);

        OPLog.Info("Export", "Export inicial de datos completado.");
    }
}
