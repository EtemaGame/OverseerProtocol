using OverseerProtocol.Core.Logging;
using OverseerProtocol.Export;
using OverseerProtocol.GameAbstractions.Catalogs;

namespace OverseerProtocol.Features;

public static class DataExportFeature
{
    public static void RunInitialExport()
    {
        OPLog.Info("Iniciando export de datos del juego...");

        var itemReader = new ItemCatalogReader();
        var itemExporter = new ItemExporter();

        var items = itemReader.ReadAllItems();
        itemExporter.ExportAll(items);

        OPLog.Info("Export inicial de items completado.");
    }
}
