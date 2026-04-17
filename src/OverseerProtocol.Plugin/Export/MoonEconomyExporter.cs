using System.Collections.Generic;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Economy;

namespace OverseerProtocol.Export
{
    public sealed class MoonEconomyExporter
    {
        public void ExportAll(List<MoonEconomyProfile> profiles)
        {
            if (profiles == null || profiles.Count == 0)
            {
                OPLog.Warning("Export", "No moon economy profiles to export.");
                return;
            }

            var path = OPPaths.MoonEconomyExportPath;
            JsonFileWriter.Write(path, profiles);
            OPLog.Info("Export", $"Wrote moon economy profiles: count={profiles.Count}, path={path}");
        }
    }
}
