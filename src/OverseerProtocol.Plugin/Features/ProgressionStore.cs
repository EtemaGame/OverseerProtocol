using System;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Progression;

namespace OverseerProtocol.Features;

public sealed class ProgressionStore
{
    private const int ExperiencePerLevel = 100;

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

    public ProgressionData GrantShipExperience(int amount)
    {
        var data = LoadOrCreate();
        var safeAmount = Math.Max(0, amount);
        data.Ship.Experience += safeAmount;

        while (data.Ship.Experience >= ExperiencePerLevel)
        {
            data.Ship.Experience -= ExperiencePerLevel;
            data.Ship.Level++;
            data.Ship.UnspentPoints++;
        }

        Save(data);
        OPLog.Info("Progression", $"Granted {safeAmount} ship XP. level={data.Ship.Level}, xp={data.Ship.Experience}, points={data.Ship.UnspentPoints}");
        return data;
    }

    public ProgressionData ResetShipProgression()
    {
        var data = LoadOrCreate();
        data.Ship = new ShipProgressionData();
        Save(data);
        OPLog.Info("Progression", "Reset ship progression.");
        return data;
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
