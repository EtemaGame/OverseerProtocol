using System;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.Features.HostFlow;

internal sealed class OverseerHostStartAdapter
{
    public HostFlowOperationResult Begin(EffectiveHostSessionPlan plan)
    {
        try
        {
            var manager = GameNetworkManager.Instance;
            if (manager == null)
            {
                return new HostFlowOperationResult(
                    Success: false,
                    CanContinue: false,
                    FailureStage: HostFailureStage.StartHost,
                    Errors: new[] { "GameNetworkManager.Instance is not available; host start aborted." },
                    Warnings: Array.Empty<string>(),
                    TelemetryCode: "host_start_manager_missing");
            }

            manager.lobbyHostSettings = new HostSettings(
                plan.Vanilla.LobbyName,
                plan.Vanilla.IsPublic,
                plan.Vanilla.LobbyTag);
            manager.StartHost();
            OPLog.Info("HostFlow", "Started host through Overseer host start adapter.");

            return new HostFlowOperationResult(
                Success: true,
                CanContinue: true,
                FailureStage: HostFailureStage.None,
                Errors: Array.Empty<string>(),
                Warnings: Array.Empty<string>());
        }
        catch (Exception ex)
        {
            OPLog.Warning("HostFlow", "Host start failed: " + ex);
            return new HostFlowOperationResult(
                Success: false,
                CanContinue: false,
                FailureStage: HostFailureStage.StartHost,
                Errors: new[] { "Could not start host: " + ex.Message },
                Warnings: Array.Empty<string>(),
                Exception: ex,
                TelemetryCode: "host_start_failed");
        }
    }
}
