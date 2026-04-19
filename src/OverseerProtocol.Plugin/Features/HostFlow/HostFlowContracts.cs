using System;
using System.Collections.Generic;
using OverseerProtocol.Features;
using UnityEngine;

namespace OverseerProtocol.Features.HostFlow;

internal enum HostFlowState
{
    Idle,
    Editing,
    ConfirmingWarnings,
    Validating,
    ApplyingPreHost,
    StartingHost,
    Started,
    Failed
}

internal enum HostFailureStage
{
    None,
    AssetBootstrap,
    VanillaInput,
    BuildReadModel,
    BuildPlan,
    Validation,
    RuntimeApply,
    MultiplayerPreHostApply,
    StartHost,
    MultiplayerPostHostApply,
    QuickMenuUx
}

internal enum VanillaHostInputFailureReason
{
    None,
    MenuUnavailable,
    CanvasUnavailable,
    HostSettingsScreenUnavailable,
    EmptyLobbyName,
    UnexpectedError
}

internal sealed record HostFlowOperationResult(
    bool Success,
    bool CanContinue,
    HostFailureStage FailureStage,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    Exception? Exception = null,
    string TelemetryCode = "");

internal sealed record HostVanillaInput(
    MenuManager MenuManager,
    string LobbyName,
    bool IsPublic,
    string LobbyTag,
    string OriginalLobbyName,
    string OriginalLobbyTag,
    string OriginalTipText,
    GameObject HostSettingsScreen,
    GameObject? MenuButtonsRoot,
    bool MenuButtonsWereActive,
    Transform CanvasRoot);

internal sealed record PresetSummary(string Id, string DisplayName, bool IsBuiltIn);

internal sealed record OverseerHostProfile(string ActivePresetId, TuningDraftStore Draft);

internal sealed record OverseerHostReadModel(
    string ActivePresetId,
    IReadOnlyList<PresetSummary> BuiltInPresets,
    IReadOnlyList<PresetSummary> CustomPresets,
    OverseerHostProfile InitialDraft,
    IReadOnlyList<string> InitialWarnings);

internal sealed record EffectiveHostSessionPlan(
    HostVanillaInput Vanilla,
    OverseerHostProfile Profile,
    RuntimeApplyPlan Runtime,
    MultiplayerHostPlan Multiplayer,
    FingerprintSnapshot PreMaterializationFingerprints,
    IReadOnlyList<string> BuildWarnings);

internal sealed record RuntimeApplyPlan(
    HostDraftSnapshot Draft,
    bool ShouldMaterializeConfig,
    bool RuntimeApplicationDeferred,
    string ActivePresetId);

internal sealed record HostDraftSnapshot(
    int SchemaVersion,
    string ActivePresetId,
    LobbySettingsDraftSnapshot Lobby,
    IReadOnlyList<MoonDraftSnapshot> Moons,
    IReadOnlyList<ItemDraftSnapshot> Items,
    IReadOnlyList<SpawnDraftSnapshot> Spawns);

internal sealed record LobbySettingsDraftSnapshot(
    string PresetId,
    bool EnableMultiplayer,
    int MaxPlayers,
    bool EnableLateJoin,
    bool LateJoinInOrbit,
    bool LateJoinOnMoonAsSpectator,
    bool RequireSameModVersion,
    bool RequireSameConfigHash);

internal sealed record MoonDraftSnapshot(
    string Id,
    string DisplayName,
    bool Enabled,
    int RoutePrice,
    int RiskLevel,
    string RiskLabel,
    string Description,
    int MinScrap,
    int MaxScrap,
    int MinTotalScrapValue,
    int MaxTotalScrapValue);

internal sealed record ItemDraftSnapshot(
    string Id,
    string DisplayName,
    bool Enabled,
    int Value,
    bool InStore,
    float Weight,
    bool IsScrap,
    int MinScrapValue,
    int MaxScrapValue,
    bool RequiresBattery,
    bool Conductive,
    bool TwoHanded);

internal sealed record SpawnDraftSnapshot(
    string MoonId,
    bool InsideEnabled,
    string InsideEnemies,
    bool OutsideEnabled,
    string OutsideEnemies,
    bool DaytimeEnabled,
    string DaytimeEnemies);

internal sealed record MultiplayerHostPlan(
    bool EnableMultiplayer,
    int MaxPlayers,
    bool EnableLateJoin,
    bool LateJoinInOrbit,
    bool LateJoinOnMoonAsSpectator,
    bool RequireSameModVersion,
    bool RequireSameConfigHash);

internal sealed record FingerprintSnapshot(
    string ActivePreset,
    string PresetFingerprint,
    string ConfigFingerprint,
    string Phase);

internal sealed record HostPlanValidationResult(
    bool Success,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InformationalWarnings,
    IReadOnlyList<string> ConfirmableWarnings,
    string TelemetryCode = "");

internal interface IOverseerHostScreen
{
    event Action<OverseerHostProfile>? ContinueRequested;
    event Action<OverseerHostProfile>? WarningsConfirmed;
    event Action? CancelRequested;
    event Action<OverseerHostProfile>? SaveRequested;

    void Open(OverseerHostReadModel model);
    void Close();
    void SetBusy(bool busy);
    void ShowError(string message);
    void ShowStatus(string message);
    void ShowWarnings(IReadOnlyList<string> warnings);
}
