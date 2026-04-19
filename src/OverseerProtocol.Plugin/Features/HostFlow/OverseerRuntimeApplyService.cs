using System;
using BepInEx.Configuration;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.Features.HostFlow;

internal sealed class OverseerRuntimeApplyService
{
    public HostFlowOperationResult ApplyPreHost(EffectiveHostSessionPlan plan)
    {
        try
        {
            if (plan == null)
                return Failure("Host plan is missing.");

            MaterializeConfig(plan.Runtime.Draft);
            var post = new FingerprintFeature().ComputeCurrent();
            OPLog.Info(
                "HostFlow",
                "Pre-host config materialized. Fingerprints: pre preset=" +
                plan.PreMaterializationFingerprints.PresetFingerprint +
                ", pre config=" +
                plan.PreMaterializationFingerprints.ConfigFingerprint +
                ", post preset=" +
                post.PresetFingerprint +
                ", post config=" +
                post.ConfigFingerprint);

            return new HostFlowOperationResult(
                Success: true,
                CanContinue: true,
                FailureStage: HostFailureStage.None,
                Errors: Array.Empty<string>(),
                Warnings: new[] { "Pre-host config materialized; runtime application is deferred to startup pipeline." },
                TelemetryCode: "host_runtime_apply_deferred");
        }
        catch (Exception ex)
        {
            OPLog.Warning("HostFlow", "Pre-host runtime materialization failed: " + ex);
            return new HostFlowOperationResult(
                Success: false,
                CanContinue: false,
                FailureStage: HostFailureStage.RuntimeApply,
                Errors: new[] { "Could not materialize host config: " + ex.Message },
                Warnings: Array.Empty<string>(),
                Exception: ex,
                TelemetryCode: "host_runtime_materialize_failed");
        }
    }

    private static void MaterializeConfig(HostDraftSnapshot draft)
    {
        OPConfig.ActivePreset.Value = draft.ActivePresetId;
        OPConfig.LastLobbyPreset.Value = draft.ActivePresetId;
        OPConfig.EnableMultiplayer.Value = draft.Lobby.EnableMultiplayer;
        OPConfig.MaxPlayers.Value = Clamp(draft.Lobby.MaxPlayers, 1, 64);
        OPConfig.EnableLateJoin.Value = draft.Lobby.EnableLateJoin;
        OPConfig.LateJoinInOrbit.Value = draft.Lobby.LateJoinInOrbit;
        OPConfig.LateJoinOnMoonAsSpectator.Value = draft.Lobby.LateJoinOnMoonAsSpectator;
        OPConfig.RequireSameModVersion.Value = draft.Lobby.RequireSameModVersion;
        OPConfig.RequireSameConfigHash.Value = draft.Lobby.RequireSameConfigHash;

        foreach (var moon in draft.Moons)
            SaveMoon(moon);

        foreach (var item in draft.Items)
            SaveItem(item);

        foreach (var spawn in draft.Spawns)
            SaveSpawn(spawn);

        OPConfig.ConfigFile.Save();
    }

    private static void SaveMoon(MoonDraftSnapshot moon)
    {
        var section = "Moons." + moon.Id;
        BindBool(section, "Enabled", false, "Enable edits for this moon.").Value = moon.Enabled;
        BindString(section, "DisplayName", moon.DisplayName, "Observed moon display name. Informational.").Value = moon.DisplayName;
        BindInt(section, "RoutePrice", moon.RoutePrice, "Terminal route price.", 0, 99999).Value = moon.RoutePrice;
        BindString(section, "Tier", moon.RiskLabel, "Risk label.").Value = moon.RiskLabel;
        BindInt(section, "RiskLevel", moon.RiskLevel, "Numeric risk level.", 0, 5).Value = moon.RiskLevel;
        BindString(section, "Description", moon.Description, "Moon description text.").Value = moon.Description;
        BindInt(section, "MinScrap", moon.MinScrap, "Minimum scrap items.", 0, 999).Value = moon.MinScrap;
        BindInt(section, "MaxScrap", moon.MaxScrap, "Maximum scrap items.", 0, 999).Value = moon.MaxScrap;
        BindInt(section, "MinTotalScrapValue", moon.MinTotalScrapValue, "Minimum total scrap value.", 0, 99999).Value = moon.MinTotalScrapValue;
        BindInt(section, "MaxTotalScrapValue", moon.MaxTotalScrapValue, "Maximum total scrap value.", 0, 99999).Value = moon.MaxTotalScrapValue;
    }

    private static void SaveItem(ItemDraftSnapshot item)
    {
        var section = "Items." + item.Id;
        BindBool(section, "Enabled", false, "Enable edits for this item.").Value = item.Enabled;
        BindString(section, "DisplayName", item.DisplayName, "Observed display name. Informational.").Value = item.DisplayName;
        BindInt(section, "Value", item.Value, "Item value/store price.", 0, 99999).Value = item.Value;
        BindFloat(section, "Weight", item.Weight, "Item weight multiplier.", 0f, 100f).Value = item.Weight;
        BindBool(section, "IsScrap", item.IsScrap, "Whether the item is scrap.").Value = item.IsScrap;
        BindBool(section, "InStore", false, "Whether this item should be present in the Terminal store.");
        BindInt(section, "StorePrice", item.Value, "Price used when the item is in the Terminal store.", 0, 99999).Value = item.Value;
        BindInt(section, "MinScrapValue", item.MinScrapValue, "Minimum scrap value.", 0, 99999).Value = item.MinScrapValue;
        BindInt(section, "MaxScrapValue", item.MaxScrapValue, "Maximum scrap value.", 0, 99999).Value = item.MaxScrapValue;
        BindBool(section, "RequiresBattery", item.RequiresBattery, "Whether the item uses battery.").Value = item.RequiresBattery;
        BindBool(section, "Conductive", item.Conductive, "Whether this item conducts lightning.").Value = item.Conductive;
        BindBool(section, "TwoHanded", item.TwoHanded, "Whether this item uses both hands.").Value = item.TwoHanded;
    }

    private static void SaveSpawn(SpawnDraftSnapshot spawn)
    {
        var section = "Moons." + spawn.MoonId;
        BindBool(section, "InsideEnemiesEnabled", false, "Enable replacement for InsideEnemies.").Value = spawn.InsideEnabled;
        BindString(section, "InsideEnemies", spawn.InsideEnemies, "EnemyId:rarity entries.").Value = spawn.InsideEnemies;
        BindBool(section, "OutsideEnemiesEnabled", false, "Enable replacement for OutsideEnemies.").Value = spawn.OutsideEnabled;
        BindString(section, "OutsideEnemies", spawn.OutsideEnemies, "EnemyId:rarity entries.").Value = spawn.OutsideEnemies;
        BindBool(section, "DaytimeEnemiesEnabled", false, "Enable replacement for DaytimeEnemies.").Value = spawn.DaytimeEnabled;
        BindString(section, "DaytimeEnemies", spawn.DaytimeEnemies, "EnemyId:rarity entries.").Value = spawn.DaytimeEnemies;
    }

    private static ConfigEntry<bool> BindBool(string section, string key, bool value, string description) =>
        OPConfig.ConfigFile.Bind(section, key, value, description);

    private static ConfigEntry<string> BindString(string section, string key, string value, string description) =>
        OPConfig.ConfigFile.Bind(section, key, value ?? "", description);

    private static ConfigEntry<int> BindInt(string section, string key, int value, string description, int min, int max) =>
        OPConfig.ConfigFile.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<int>(min, max)));

    private static ConfigEntry<float> BindFloat(string section, string key, float value, string description, float min, float max) =>
        OPConfig.ConfigFile.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));

    private static HostFlowOperationResult Failure(string error) =>
        new(
            Success: false,
            CanContinue: false,
            FailureStage: HostFailureStage.RuntimeApply,
            Errors: new[] { error },
            Warnings: Array.Empty<string>(),
            TelemetryCode: "host_runtime_missing_plan");

    private static int Clamp(int value, int min, int max) =>
        Math.Max(min, Math.Min(max, value));
}
