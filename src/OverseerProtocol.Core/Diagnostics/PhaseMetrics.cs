using System;
using System.Diagnostics;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.Core.Diagnostics;

public sealed class PhaseMetrics
{
    private readonly string _category;
    private readonly string _phase;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    private int _applied;
    private int _skipped;
    private int _warnings;
    private int _errors;

    private PhaseMetrics(string category, string phase)
    {
        _category = category;
        _phase = phase;
        OPLog.Info(_category, $"Phase '{_phase}' started.");
    }

    public static PhaseMetrics Start(string category, string phase) =>
        new(category, phase);

    public void Applied(int count = 1) =>
        _applied += Math.Max(0, count);

    public void Skipped(int count = 1) =>
        _skipped += Math.Max(0, count);

    public void Warning(int count = 1) =>
        _warnings += Math.Max(0, count);

    public void Error(int count = 1) =>
        _errors += Math.Max(0, count);

    public void Complete()
    {
        _stopwatch.Stop();
        OPLog.Info(
            _category,
            $"Phase '{_phase}' completed in {_stopwatch.ElapsedMilliseconds}ms. applied={_applied}, skipped={_skipped}, warnings={_warnings}, errors={_errors}");
    }
}
