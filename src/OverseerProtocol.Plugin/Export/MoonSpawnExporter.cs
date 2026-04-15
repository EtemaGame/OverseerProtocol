using System.Collections.Generic;
using System.IO;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Spawns;

namespace OverseerProtocol.Export
{
    public sealed class MoonSpawnExporter
    {
        public void ExportAll(List<MoonSpawnProfile> profiles)
        {
            if (profiles == null || profiles.Count == 0)
            {
                OPLog.Warning("Export", "No spawn profiles to export.");
                return;
            }

            var path = Path.Combine(OPPaths.ExportRoot, "spawns", "moon-spawn-profiles.json");
            JsonFileWriter.Write(path, profiles);
            OPLog.Info("Export", $"Desplegados perfiles de spawn ({profiles.Count} registros) en {path}");
        }
    }
}
