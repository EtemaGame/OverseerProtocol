using System.Collections.Generic;
using System.IO;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Items;

namespace OverseerProtocol.Export;

public sealed class ItemExporter
{
    public int ExportAll(IReadOnlyList<ItemDefinition> items)
    {
        var path = Path.Combine(OPPaths.ItemExportRoot, "items.json");
        JsonFileWriter.Write(path, items);
        OPLog.Info($"Exportados {items.Count} items a {path}");
        return items.Count;
    }
}
