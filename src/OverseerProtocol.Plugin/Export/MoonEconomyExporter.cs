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

            JsonFileWriter.Write(OPPaths.MoonEconomyExportPath, profiles);
            OPLog.Info("Export", $"Desplegados perfiles económicos de lunas ({profiles.Count} registros) en {OPPaths.MoonEconomyExportPath}");
        }
    }
}
