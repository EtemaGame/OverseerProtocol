using System;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.Features.HostFlow;

internal sealed class OverseerHostSessionController
{
    private readonly OverseerHostScreenBootstrap _bootstrap;
    private readonly OverseerVanillaHostInputReader _inputReader;
    private readonly OverseerHostReadModelFactory _readModelFactory;
    private readonly OverseerEffectiveConfigBuilder _configBuilder;
    private readonly OverseerHostPlanValidator _validator;
    private readonly OverseerRuntimeApplyService _runtimeApplyService;
    private readonly OverseerMultiplayerApplyService _multiplayerApplyService;
    private readonly OverseerHostStartAdapter _hostStartAdapter;
    private HostVanillaInput? _currentInput;
    private IOverseerHostScreen? _screen;
    private EffectiveHostSessionPlan? _pendingWarningPlan;
    private string[] _pendingInformationalWarnings = Array.Empty<string>();
    private bool _startedHost;

    public OverseerHostSessionController(
        OverseerHostScreenBootstrap bootstrap,
        OverseerVanillaHostInputReader inputReader,
        OverseerHostReadModelFactory readModelFactory,
        OverseerEffectiveConfigBuilder configBuilder,
        OverseerHostPlanValidator validator,
        OverseerRuntimeApplyService runtimeApplyService,
        OverseerMultiplayerApplyService multiplayerApplyService,
        OverseerHostStartAdapter hostStartAdapter)
    {
        _bootstrap = bootstrap;
        _inputReader = inputReader;
        _readModelFactory = readModelFactory;
        _configBuilder = configBuilder;
        _validator = validator;
        _runtimeApplyService = runtimeApplyService;
        _multiplayerApplyService = multiplayerApplyService;
        _hostStartAdapter = hostStartAdapter;
    }

    public HostFlowState State { get; private set; } = HostFlowState.Idle;

    public bool TryBegin(HostVanillaInput input, out string error)
    {
        error = "";
        if (State != HostFlowState.Idle)
        {
            error = "Host flow is already active.";
            return false;
        }

        if (input.MenuManager == null)
        {
            error = "MenuManager is no longer alive.";
            return false;
        }

        if (!_bootstrap.TryPrepare(input.MenuManager, out var screen, out error) || screen == null)
            return false;

        var readModelResult = _readModelFactory.TryBuild(out var model);
        if (!readModelResult.Success || model == null)
        {
            error = readModelResult.Errors.Count > 0
                ? readModelResult.Errors[0]
                : "Could not build host read model.";
            return false;
        }

        _currentInput = input;
        _screen = screen;
        _startedHost = false;
        AttachScreenEvents(screen);

        State = HostFlowState.Editing;
        screen.Open(model);
        screen.SetBusy(false);
        OPLog.Info("HostFlow", "Overseer host flow opened.");
        return true;
    }

    private void AttachScreenEvents(IOverseerHostScreen screen)
    {
        DetachScreenEvents(screen);
        screen.ContinueRequested += OnContinueRequested;
        screen.WarningsConfirmed += OnWarningsConfirmed;
        screen.CancelRequested += OnCancelRequested;
    }

    private void DetachScreenEvents(IOverseerHostScreen screen)
    {
        screen.ContinueRequested -= OnContinueRequested;
        screen.WarningsConfirmed -= OnWarningsConfirmed;
        screen.CancelRequested -= OnCancelRequested;
    }

    private void OnContinueRequested(OverseerHostProfile profile)
    {
        if (State != HostFlowState.Editing)
        {
            OPLog.Debug("HostFlow", "Ignored ContinueRequested while state=" + State);
            return;
        }

        if (_screen == null)
            return;

        State = HostFlowState.Validating;
        _screen.SetBusy(true);
        _pendingWarningPlan = null;
        _pendingInformationalWarnings = Array.Empty<string>();

        if (_currentInput == null)
        {
            State = HostFlowState.Editing;
            _screen.SetBusy(false);
            _screen.ShowError("Host input is no longer available.");
            return;
        }

        var build = _configBuilder.TryBuild(_currentInput, profile, out var plan);
        if (!build.Success || plan == null)
        {
            State = HostFlowState.Editing;
            _screen.SetBusy(false);
            _screen.ShowError(FirstError(build, "Could not build host plan."));
            return;
        }

        var validation = _validator.Validate(plan);
        if (!validation.Success)
        {
            State = HostFlowState.Editing;
            _screen.SetBusy(false);
            _screen.ShowError(validation.Errors.Count > 0 ? validation.Errors[0] : "Host plan validation failed.");
            return;
        }

        _pendingInformationalWarnings = ToArray(validation.InformationalWarnings);
        if (validation.ConfirmableWarnings.Count > 0)
        {
            State = HostFlowState.ConfirmingWarnings;
            _pendingWarningPlan = plan;
            _screen.SetBusy(false);
            _screen.ShowWarnings(validation.ConfirmableWarnings);
            return;
        }

        ContinueWithValidatedPlan(plan, _pendingInformationalWarnings);
    }

    private void OnWarningsConfirmed(OverseerHostProfile profile)
    {
        if (State != HostFlowState.ConfirmingWarnings)
        {
            OPLog.Debug("HostFlow", "Ignored WarningsConfirmed while state=" + State);
            return;
        }

        if (_pendingWarningPlan == null)
        {
            State = HostFlowState.Editing;
            _screen?.ShowError("Warning confirmation was received without a pending host plan.");
            return;
        }

        ContinueWithValidatedPlan(_pendingWarningPlan, _pendingInformationalWarnings);
    }

    private void OnCancelRequested()
    {
        switch (State)
        {
            case HostFlowState.ConfirmingWarnings:
                ReturnToEditing();
                return;
            case HostFlowState.Editing:
            case HostFlowState.Failed:
                CancelEntireFlow();
                return;
            case HostFlowState.ApplyingPreHost:
            case HostFlowState.StartingHost:
                _screen?.ShowStatus("Please wait; Overseer is applying host settings.");
                return;
            default:
                OPLog.Debug("HostFlow", "Ignored CancelRequested while state=" + State);
                return;
        }
    }

    private void ReturnToEditing()
    {
        State = HostFlowState.Editing;
        _pendingWarningPlan = null;
        _pendingInformationalWarnings = Array.Empty<string>();
        _screen?.SetBusy(false);
        _screen?.ShowStatus("Returned to editing.");
    }

    private void ContinueWithValidatedPlan(EffectiveHostSessionPlan plan, string[] informationalWarnings)
    {
        if (_screen == null)
            return;

        State = HostFlowState.ApplyingPreHost;
        _screen.SetBusy(true);
        _screen.ShowStatus(BuildStatus("Applying host configuration...", informationalWarnings));

        var runtimeApply = _runtimeApplyService.ApplyPreHost(plan);
        if (!runtimeApply.Success)
        {
            ReturnToEditingWithError(FirstError(runtimeApply, "Could not apply host configuration."));
            return;
        }

        var multiplayerPreHost = _multiplayerApplyService.ApplyPreHost(plan);
        if (!multiplayerPreHost.Success)
        {
            ReturnToEditingWithError(FirstError(multiplayerPreHost, "Could not apply multiplayer pre-host settings."));
            return;
        }

        State = HostFlowState.StartingHost;
        _screen.ShowStatus(BuildStatus("Starting host...", CombineWarnings(informationalWarnings, runtimeApply.Warnings, multiplayerPreHost.Warnings)));
        var start = _hostStartAdapter.Begin(plan);
        if (!start.Success)
        {
            State = HostFlowState.Failed;
            _screen.SetBusy(false);
            _screen.ShowError(FirstError(start, "Could not start host."));
            return;
        }

        _startedHost = true;
        var multiplayerPostHost = _multiplayerApplyService.ApplyPostHost(plan);
        if (!multiplayerPostHost.Success && multiplayerPostHost.Warnings.Count > 0)
            OPLog.Warning("HostFlow", string.Join("\n", multiplayerPostHost.Warnings));

        _screen.Close();
        DetachScreenEvents(_screen);
        OPLog.Info("HostFlow", "Overseer host flow completed.");
        ResetState();
    }

    private void ReturnToEditingWithError(string error)
    {
        State = HostFlowState.Editing;
        _screen?.SetBusy(false);
        _screen?.ShowError(error);
    }

    private void CancelEntireFlow()
    {
        if (_screen != null)
        {
            _screen.Close();
            DetachScreenEvents(_screen);
        }

        if (!_startedHost && _currentInput != null)
            _inputReader.RestoreHostSettings(_currentInput);

        OPLog.Info("HostFlow", "Overseer host flow cancelled.");
        ResetState();
    }

    private void ResetState()
    {
        _currentInput = null;
        _screen = null;
        _pendingWarningPlan = null;
        _pendingInformationalWarnings = Array.Empty<string>();
        _startedHost = false;
        State = HostFlowState.Idle;
    }

    private static string FirstError(HostFlowOperationResult result, string fallback) =>
        result.Errors.Count > 0 ? result.Errors[0] : fallback;

    private static string BuildStatus(string firstLine, string[] warnings)
    {
        if (warnings.Length == 0)
            return firstLine;

        return firstLine + "\n" + string.Join("\n", warnings);
    }

    private static string[] ToArray(System.Collections.Generic.IReadOnlyList<string> values)
    {
        var result = new string[values.Count];
        for (var i = 0; i < values.Count; i++)
            result[i] = values[i];
        return result;
    }

    private static string[] CombineWarnings(params System.Collections.Generic.IReadOnlyList<string>[] warningLists)
    {
        var count = 0;
        foreach (var warnings in warningLists)
            count += warnings.Count;

        if (count == 0)
            return Array.Empty<string>();

        var result = new string[count];
        var index = 0;
        foreach (var warnings in warningLists)
        {
            for (var i = 0; i < warnings.Count; i++)
                result[index++] = warnings[i];
        }

        return result;
    }
}
