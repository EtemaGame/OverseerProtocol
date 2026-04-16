using System.Collections.Generic;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Items;

namespace OverseerProtocol.Export
{
    public sealed class ItemExporter
    {
        public void ExportAll(List<ItemDefinition> items)
        {
            if (items == null || items.Count == 0)
            {
                OPLog.Warning("Export", "No items to export.");
                return;
            }

            var path = OPPaths.ItemExportPath;
            JsonFileWriter.Write(path, items);
            OPLog.Info("Export", $"Desplegado catálogo de items ({items.Count} registros) en {path}");
        }
    }
}
