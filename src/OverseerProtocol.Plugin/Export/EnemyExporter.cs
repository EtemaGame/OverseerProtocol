using System.Collections.Generic;
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

            var path = OPPaths.EnemyExportPath;
            JsonFileWriter.Write(path, enemies);
            OPLog.Info("Export", $"Wrote enemy catalog: count={enemies.Count}, path={path}");
        }
    }
}
