using System;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.Features.HostFlow;

internal sealed class OverseerMultiplayerApplyService
{
    public HostFlowOperationResult ApplyPreHost(EffectiveHostSessionPlan plan)
    {
        try
        {
            if (GameNetworkManager.Instance == null)
            {
                return new HostFlowOperationResult(
                    Success: true,
                    CanContinue: true,
                    FailureStage: HostFailureStage.None,
                    Errors: Array.Empty<string>(),
                    Warnings: new[] { "GameNetworkManager unavailable; multiplayer pre-host apply deferred." },
                    TelemetryCode: "host_multiplayer_pre_deferred");
            }

            new MultiplayerFeature().ApplyMaxPlayers();
            return Success(Array.Empty<string>());
        }
        catch (Exception ex)
        {
            OPLog.Warning("HostFlow", "Multiplayer pre-host apply failed: " + ex);
            return new HostFlowOperationResult(
                Success: false,
                CanContinue: false,
                FailureStage: HostFailureStage.MultiplayerPreHostApply,
                Errors: new[] { "Could not apply multiplayer pre-host settings: " + ex.Message },
                Warnings: Array.Empty<string>(),
                Exception: ex,
                TelemetryCode: "host_multiplayer_pre_failed");
        }
    }

    public HostFlowOperationResult ApplyPostHost(EffectiveHostSessionPlan plan)
    {
        try
        {
            new MultiplayerFeature().Apply();
            return Success(Array.Empty<string>());
        }
        catch (Exception ex)
        {
            OPLog.Warning("HostFlow", "Multiplayer post-host apply failed: " + ex);
            return new HostFlowOperationResult(
                Success: false,
                CanContinue: true,
                FailureStage: HostFailureStage.MultiplayerPostHostApply,
                Errors: Array.Empty<string>(),
                Warnings: new[] { "Host started, but multiplayer post-host metadata refresh failed: " + ex.Message },
                Exception: ex,
                TelemetryCode: "host_multiplayer_post_failed");
        }
    }

    private static HostFlowOperationResult Success(string[] warnings) =>
        new(
            Success: true,
            CanContinue: true,
            FailureStage: HostFailureStage.None,
            Errors: Array.Empty<string>(),
            Warnings: warnings);
}
