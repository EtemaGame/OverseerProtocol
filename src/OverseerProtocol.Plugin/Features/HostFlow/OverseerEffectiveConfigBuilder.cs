using System;
using System.Collections.Generic;
using System.Linq;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.Features.HostFlow;

internal sealed class OverseerEffectiveConfigBuilder
{
    private const int HostDraftSchemaVersion = 1;

    public HostFlowOperationResult TryBuild(
        HostVanillaInput input,
        OverseerHostProfile profile,
        out EffectiveHostSessionPlan? plan)
    {
        plan = null;
        try
        {
            if (input == null)
                return Failure("Host input is missing.");

            if (profile == null || profile.Draft == null)
                return Failure("Host profile is missing.");

            var buildWarnings = new List<string>();
            if (!TryResolvePreset(profile.ActivePresetId, buildWarnings, out var activePreset, out var presetError))
                return Failure(presetError);

            var draft = profile.Draft;

            var snapshot = SnapshotDraft(draft, activePreset.Id);
            var fingerprints = new FingerprintFeature().ComputeCurrent();
            var preMaterializationFingerprints = new FingerprintSnapshot(
                fingerprints.ActivePreset,
                fingerprints.PresetFingerprint,
                fingerprints.ConfigFingerprint,
                "pre-materialization");

            var multiplayer = new MultiplayerHostPlan(
                draft.Lobby.EnableMultiplayer,
                Clamp(draft.Lobby.MaxPlayers, 1, 64),
                draft.Lobby.EnableLateJoin,
                draft.Lobby.LateJoinInOrbit,
                draft.Lobby.LateJoinOnMoonAsSpectator,
                draft.Lobby.RequireSameModVersion,
                draft.Lobby.RequireSameConfigHash);

            plan = new EffectiveHostSessionPlan(
                input,
                profile,
                new RuntimeApplyPlan(snapshot, ShouldMaterializeConfig: true, RuntimeApplicationDeferred: true, activePreset.Id),
                multiplayer,
                preMaterializationFingerprints,
                buildWarnings);

            return new HostFlowOperationResult(
                Success: true,
                CanContinue: true,
                FailureStage: HostFailureStage.None,
                Errors: Array.Empty<string>(),
                Warnings: buildWarnings);
        }
        catch (Exception ex)
        {
            OPLog.Warning("HostFlow", "Could not build effective host plan: " + ex);
            return new HostFlowOperationResult(
                Success: false,
                CanContinue: false,
                FailureStage: HostFailureStage.BuildPlan,
                Errors: new[] { "Could not build effective host plan: " + ex.Message },
                Warnings: Array.Empty<string>(),
                Exception: ex,
                TelemetryCode: "host_plan_build_failed");
        }
    }

    private static bool TryResolvePreset(string presetId, List<string> warnings, out LobbyPresetDefinition preset, out string error)
    {
        preset = null!;
        error = "";
        var requested = string.IsNullOrWhiteSpace(presetId) ? OPConfig.DefaultPreset : presetId.Trim();
        var match = LobbyPresetDefinition.All.FirstOrDefault(preset =>
            string.Equals(preset.Id, requested, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            preset = match;
            return true;
        }

        warnings.Add("Preset '" + requested + "' was not found; default will be used.");
        var defaultPreset = LobbyPresetDefinition.All.FirstOrDefault(preset =>
            string.Equals(preset.Id, OPConfig.DefaultPreset, StringComparison.OrdinalIgnoreCase));
        if (defaultPreset == null)
        {
            error = "Default preset '" + OPConfig.DefaultPreset + "' was not found.";
            return false;
        }

        preset = defaultPreset;
        return true;
    }

    private static HostDraftSnapshot SnapshotDraft(TuningDraftStore draft, string activePresetId) =>
        new(
            HostDraftSchemaVersion,
            activePresetId,
            new LobbySettingsDraftSnapshot(
                activePresetId,
                draft.Lobby.EnableMultiplayer,
                Clamp(draft.Lobby.MaxPlayers, 1, 64),
                draft.Lobby.EnableLateJoin,
                draft.Lobby.LateJoinInOrbit,
                draft.Lobby.LateJoinOnMoonAsSpectator,
                draft.Lobby.RequireSameModVersion,
                draft.Lobby.RequireSameConfigHash),
            draft.Moons.Select(moon => new MoonDraftSnapshot(
                    moon.Baseline.Id,
                    moon.Baseline.DisplayName,
                    moon.Enabled,
                    moon.RoutePrice,
                    moon.RiskLevel,
                    moon.RiskLabel,
                    moon.Description,
                    moon.MinScrap,
                    moon.MaxScrap,
                    moon.MinTotalScrapValue,
                    moon.MaxTotalScrapValue))
                .ToList(),
            draft.Items.Select(item => new ItemDraftSnapshot(
                    item.Baseline.Id,
                    item.Baseline.DisplayName,
                    item.Enabled,
                    item.Value,
                    InStore: false,
                    item.Weight,
                    item.IsScrap,
                    item.MinScrapValue,
                    item.MaxScrapValue,
                    item.RequiresBattery,
                    item.Conductive,
                    item.TwoHanded))
                .ToList(),
            draft.Spawns.Select(spawn => new SpawnDraftSnapshot(
                    spawn.Baseline.MoonId,
                    spawn.InsideEnabled,
                    spawn.InsideEnemies,
                    spawn.OutsideEnabled,
                    spawn.OutsideEnemies,
                    spawn.DaytimeEnabled,
                    spawn.DaytimeEnemies))
                .ToList());

    private static HostFlowOperationResult Failure(string error) =>
        new(
            Success: false,
            CanContinue: false,
            FailureStage: HostFailureStage.BuildPlan,
            Errors: new[] { error },
            Warnings: Array.Empty<string>(),
            TelemetryCode: "host_plan_missing_input");

    private static int Clamp(int value, int min, int max) =>
        Math.Max(min, Math.Min(max, value));
}
