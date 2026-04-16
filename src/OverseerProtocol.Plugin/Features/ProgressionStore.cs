using System;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Progression;

namespace OverseerProtocol.Features;

public sealed class ProgressionStore
{
    public ProgressionData LoadOrCreate()
    {
        var data = JsonFileReader.Read<ProgressionData>(OPPaths.ProgressionSavePath);
        if (data != null)
            return Normalize(data);

        data = CreateDefault();
        Save(data);
        OPLog.Info("Progression", $"Created progression save at {OPPaths.ProgressionSavePath}");
        return data;
    }

    public void Save(ProgressionData data)
    {
        data.LastUpdatedUtc = DateTime.UtcNow.ToString("O");
        JsonFileWriter.Write(OPPaths.ProgressionSavePath, data);
    }

    private static ProgressionData CreateDefault() =>
        new ProgressionData
        {
            SaveId = "default",
            ActivePreset = OPConfig.ActivePresetName
        };

    private static ProgressionData Normalize(ProgressionData data)
    {
        if (data.SchemaVersion <= 0)
            data.SchemaVersion = 1;

        if (string.IsNullOrWhiteSpace(data.SaveId))
            data.SaveId = "default";

        if (string.IsNullOrWhiteSpace(data.ActivePreset))
            data.ActivePreset = OPConfig.ActivePresetName;

        data.Ship ??= new ShipProgressionData();
        data.Players ??= new System.Collections.Generic.List<PlayerProgressionData>();

        return data;
    }
}
