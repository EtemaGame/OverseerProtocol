using System;
using System.Collections.Generic;

namespace OverseerProtocol.Features.HostFlow;

internal sealed class OverseerHostPlanValidator
{
    public HostPlanValidationResult Validate(EffectiveHostSessionPlan plan)
    {
        var errors = new List<string>();
        var informationalWarnings = new List<string>();
        var confirmableWarnings = new List<string>();
        var telemetryCode = "";

        if (plan == null)
        {
            errors.Add("Host plan is missing.");
            return new HostPlanValidationResult(false, errors, informationalWarnings, confirmableWarnings, "host_plan_missing");
        }

        if (string.IsNullOrWhiteSpace(plan.Vanilla.LobbyName))
            errors.Add("Lobby name is empty.");

        if (plan.Multiplayer.MaxPlayers < 1 || plan.Multiplayer.MaxPlayers > 64)
            errors.Add("Max players must be between 1 and 64.");

        if (plan.Multiplayer.EnableLateJoin && !plan.Multiplayer.EnableMultiplayer)
            errors.Add("Late join requires multiplayer patches to be enabled.");

        foreach (var spawn in plan.Runtime.Draft.Spawns)
        {
            if (!SpawnDraft.LooksLikeSpawnList(spawn.InsideEnemies))
                errors.Add("Invalid inside spawn list for " + spawn.MoonId + ". Use EnemyId:rarity, EnemyId:rarity.");
            if (!SpawnDraft.LooksLikeSpawnList(spawn.OutsideEnemies))
                errors.Add("Invalid outside spawn list for " + spawn.MoonId + ". Use EnemyId:rarity, EnemyId:rarity.");
            if (!SpawnDraft.LooksLikeSpawnList(spawn.DaytimeEnemies))
                errors.Add("Invalid daytime spawn list for " + spawn.MoonId + ". Use EnemyId:rarity, EnemyId:rarity.");
        }

        if (plan.Runtime.Draft.Moons.Count == 0 && plan.Runtime.Draft.Items.Count == 0 && plan.Runtime.Draft.Spawns.Count == 0)
        {
            informationalWarnings.Add("Runtime catalogs are empty; host setup can continue, but runtime tabs may be incomplete.");
            telemetryCode = "host_flow_empty_catalogs";
        }

        informationalWarnings.AddRange(plan.BuildWarnings);

        if (plan.Multiplayer.LateJoinOnMoonAsSpectator)
            confirmableWarnings.Add("Late join on moon as spectator is experimental and depends on player lifecycle hooks.");

        if (plan.Multiplayer.RequireSameConfigHash)
            confirmableWarnings.Add("Require same config hash will reject clients whose Overseer config does not match exactly.");

        return new HostPlanValidationResult(
            errors.Count == 0,
            errors,
            informationalWarnings,
            confirmableWarnings,
            telemetryCode);
    }
}
