using System;
using System.Collections.Generic;
using System.Linq;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.Features.HostFlow;

internal sealed class OverseerHostReadModelFactory
{
    public HostFlowOperationResult TryBuild(out OverseerHostReadModel? model)
    {
        model = null;
        try
        {
            var draft = TuningDraftStore.Load();
            var activePreset = LobbyPresetDefinition.Resolve(draft.Lobby.PresetId);
            var warnings = new List<string>();
            if (draft.Moons.Count == 0)
                warnings.Add("No moon catalog is available yet. Runtime moon tabs will be incomplete.");
            if (draft.Items.Count == 0)
                warnings.Add("No item catalog is available yet. Runtime item tabs will be incomplete.");
            if (draft.Spawns.Count == 0)
                warnings.Add("No spawn catalog is available yet. Runtime spawn tabs will be incomplete.");

            var builtIns = LobbyPresetDefinition.All
                .Select(preset => new PresetSummary(preset.Id, preset.DisplayName, true))
                .ToList();

            model = new OverseerHostReadModel(
                activePreset.Id,
                builtIns,
                Array.Empty<PresetSummary>(),
                new OverseerHostProfile(activePreset.Id, draft),
                warnings);

            return new HostFlowOperationResult(
                Success: true,
                CanContinue: true,
                FailureStage: HostFailureStage.None,
                Errors: Array.Empty<string>(),
                Warnings: warnings);
        }
        catch (Exception ex)
        {
            OPLog.Warning("HostFlow", "Could not build host read model: " + ex);
            return new HostFlowOperationResult(
                Success: false,
                CanContinue: false,
                FailureStage: HostFailureStage.BuildReadModel,
                Errors: new[] { "Could not build Overseer host screen data: " + ex.Message },
                Warnings: Array.Empty<string>(),
                Exception: ex,
                TelemetryCode: "host_read_model_build_failed");
        }
    }
}
