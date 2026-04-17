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
        {
            var normalized = Normalize(data);
            OPLog.Info(
                "Progression",
                $"Loaded progression save from {OPPaths.ProgressionSavePath}: schemaVersion={normalized.SchemaVersion}, saveId={normalized.SaveId}, activePreset={normalized.ActivePreset}, shipLevel={normalized.Ship.Level}, shipXp={normalized.Ship.Experience}, players={normalized.Players.Count}");
            return normalized;
        }

        data = CreateDefault();
        Save(data);
        OPLog.Info("Progression", $"Created progression save at {OPPaths.ProgressionSavePath}");
        return data;
    }

    public void Save(ProgressionData data)
    {
        data.LastUpdatedUtc = DateTime.UtcNow.ToString("O");
        JsonFileWriter.Write(OPPaths.ProgressionSavePath, data);
        OPLog.Info(
            "Progression",
            $"Saved progression: schemaVersion={data.SchemaVersion}, saveId={data.SaveId}, activePreset={data.ActivePreset}, shipLevel={data.Ship.Level}, shipXp={data.Ship.Experience}, points={data.Ship.UnspentPoints}, players={data.Players.Count}");
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
        {
            OPLog.Info("Progression", $"Normalizing progression schemaVersion {data.SchemaVersion} -> 1");
            data.SchemaVersion = 1;
        }

        if (string.IsNullOrWhiteSpace(data.SaveId))
        {
            OPLog.Info("Progression", "Normalizing empty SaveId -> default");
            data.SaveId = "default";
        }

        if (string.IsNullOrWhiteSpace(data.ActivePreset))
        {
            OPLog.Info("Progression", $"Normalizing empty ActivePreset -> {OPConfig.ActivePresetName}");
            data.ActivePreset = OPConfig.ActivePresetName;
        }

        data.Ship ??= new ShipProgressionData();
        data.Players ??= new System.Collections.Generic.List<PlayerProgressionData>();

        return data;
    }
}
