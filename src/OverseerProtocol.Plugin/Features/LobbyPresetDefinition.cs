using System;
using System.Collections.Generic;
using OverseerProtocol.Configuration;

namespace OverseerProtocol.Features;

internal sealed class LobbyPresetDefinition
{
    public string Id { get; private set; } = OPConfig.DefaultPreset;
    public string DisplayName { get; private set; } = "Default";
    public bool EnableMultiplayer { get; private set; }
    public int MaxPlayers { get; private set; } = 4;
    public bool EnableLateJoin { get; private set; }
    public bool LateJoinInOrbit { get; private set; } = true;
    public bool LateJoinOnMoonAsSpectator { get; private set; }
    public bool RequireSameModVersion { get; private set; } = true;
    public bool RequireSameConfigHash { get; private set; }

    public static IReadOnlyList<LobbyPresetDefinition> All { get; } =
        new[]
        {
            Create(OPConfig.DefaultPreset, "Default", false, 4, false, true, false, true, false),
            Create("vanilla-plus", "Vanilla Plus", true, 8, true, true, false, true, false),
            Create("hardcore", "Hardcore", true, 8, true, true, false, true, true),
            Create("economy-chaos", "Economy Chaos", true, 12, true, true, false, true, false),
            Create("outside-nightmare", "Outside Nightmare", true, 12, true, true, true, true, true),
            Create("scrap-heaven", "Scrap Heaven", true, 10, true, true, false, true, false)
        };

    public static LobbyPresetDefinition Resolve(string? id)
    {
        var normalized = string.IsNullOrWhiteSpace(id) ? OPConfig.DefaultPreset : id.Trim();
        foreach (var preset in All)
        {
            if (string.Equals(preset.Id, normalized, StringComparison.OrdinalIgnoreCase))
                return preset;
        }

        return All[0];
    }

    private static LobbyPresetDefinition Create(
        string id,
        string displayName,
        bool enableMultiplayer,
        int maxPlayers,
        bool enableLateJoin,
        bool lateJoinInOrbit,
        bool lateJoinOnMoonAsSpectator,
        bool requireSameModVersion,
        bool requireSameConfigHash) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            EnableMultiplayer = enableMultiplayer,
            MaxPlayers = maxPlayers,
            EnableLateJoin = enableLateJoin,
            LateJoinInOrbit = lateJoinInOrbit,
            LateJoinOnMoonAsSpectator = lateJoinOnMoonAsSpectator,
            RequireSameModVersion = requireSameModVersion,
            RequireSameConfigHash = requireSameConfigHash
        };
}
