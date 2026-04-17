using System.Collections.Generic;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Moons;

namespace OverseerProtocol.Export
{
    public sealed class MoonExporter
    {
        public void ExportAll(List<MoonDefinition> moons)
        {
            if (moons == null || moons.Count == 0)
            {
                OPLog.Warning("Export", "No moons to export.");
                return;
            }

            var path = OPPaths.MoonExportPath;
            JsonFileWriter.Write(path, moons);
            OPLog.Info("Export", $"Wrote moon catalog: count={moons.Count}, path={path}");
        }
    }
}
