using OverseerProtocol.Configuration;
using OverseerProtocol.Data.Models.Sync;

namespace OverseerProtocol.Features;

public sealed class HandshakeFeature
{
    public ProtocolHandshakeDefinition BuildCurrent()
    {
        var fingerprints = new FingerprintFeature().ComputeCurrent();
        var lobbyRules = new LobbyRulesFeature().LoadOrCreate();

        return new ProtocolHandshakeDefinition
        {
            OverseerVersion = global::OverseerProtocol.Plugin.ModVersion,
            ActivePreset = fingerprints.ActivePreset,
            PresetFingerprint = fingerprints.PresetFingerprint,
            ConfigFingerprint = fingerprints.ConfigFingerprint,
            Rules = new LobbyHandshakeRules
            {
                MaxPlayers = lobbyRules.MaxPlayers,
                AllowLateJoin = lobbyRules.AllowLateJoin,
                LateJoinMode = lobbyRules.LateJoinMode,
                RequireMatchingOverseerVersion = lobbyRules.RequireMatchingOverseerVersion,
                RequireMatchingPreset = lobbyRules.RequireMatchingPreset
            },
            EnabledFeatures =
            {
                OPConfig.EnableItemOverrides.Value ? "item-overrides" : "item-overrides:disabled",
                OPConfig.EnableSpawnOverrides.Value ? "spawn-overrides" : "spawn-overrides:disabled",
                OPConfig.EnableMoonOverrides.Value ? "moon-overrides" : "moon-overrides:disabled",
                OPConfig.EnableRuntimeMultipliers.Value ? "runtime-multipliers" : "runtime-multipliers:disabled",
                OPConfig.EnableProgressionStorage.Value ? "progression-storage" : "progression-storage:disabled",
                OPConfig.EnablePerkCatalog.Value ? "perk-catalog" : "perk-catalog:disabled",
                OPConfig.EnableRuntimeRulesLoading.Value ? "runtime-rules" : "runtime-rules:disabled"
            }
        };
    }
}
