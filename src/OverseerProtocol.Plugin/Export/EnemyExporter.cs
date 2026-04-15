using System.Collections.Generic;
using System.IO;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Enemies;

namespace OverseerProtocol.Export
{
    public sealed class EnemyExporter
    {
        public void ExportAll(List<EnemyDefinition> enemies)
        {
            if (enemies == null || enemies.Count == 0)
            {
                OPLog.Warning("Export", "No enemies to export.");
                return;
            }

            var path = Path.Combine(OPPaths.EnemyExportRoot, "enemies.json");
            JsonFileWriter.Write(path, enemies);
            OPLog.Info("Export", $"Desplegado catálogo de enemigos ({enemies.Count} registros) en {path}");
        }
    }
}
