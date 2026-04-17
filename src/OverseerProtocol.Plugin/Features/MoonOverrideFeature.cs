using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.GameAbstractions.Overrides;
using UnityEngine;

namespace OverseerProtocol.Features;

public sealed class MoonOverrideFeature
{
    private readonly MoonOverrideValidator _validator;
    private readonly MoonOverrideApplier _applier;

    public MoonOverrideFeature()
    {
        _validator = new MoonOverrideValidator();
        _applier = new MoonOverrideApplier();
    }

    public void ApplyOverrides(string presetName)
    {
        OPLog.Info("Overrides", "Loading moon tuning from BepInEx .cfg.");
        var collection = new RuntimeTuningConfig(OPConfig.ConfigFile).BuildMoonOverrides();

        if (collection.Overrides.Count == 0)
        {
            OPLog.Info("Overrides", "No active moon runtime tuning entries found in .cfg.");
            return;
        }

        OPLog.Info("Overrides", $"Moon .cfg tuning loaded: moonTuningCount={collection.Overrides.Count}");

        var references = LoadReferenceCatalog();
        OPLog.Info("Validation", $"Moon validation references: runtimeMoons={references.RuntimeMoonIds.Count}, routePriceMoons={references.RoutePriceMoonIds.Count}");
        var validationResult = _validator.Validate(collection, references);
        validationResult.Report.WriteToLog("Validation");

        var abortOnWarnings = OPConfig.StrictValidation.Value || OPConfig.AbortOnInvalidOverrideBlock.Value;
        OPLog.Info("Validation", $"Moon validation policy: strict={OPConfig.StrictValidation.Value}, abortOnInvalidBlock={OPConfig.AbortOnInvalidOverrideBlock.Value}, abortOnWarnings={abortOnWarnings}, canApply={validationResult.CanApplyWithStrictMode(abortOnWarnings)}");
        if (!validationResult.CanApplyWithStrictMode(abortOnWarnings))
        {
            var reason = abortOnWarnings && validationResult.Report.WarningCount > 0
                ? "strict tuning validation is enabled and warnings were reported"
                : "critical validation errors were reported";

            OPLog.Warning("Validation", $"Moon tuning was not applied because {reason}.");
            return;
        }

        if (OPConfig.DryRunOverrides.Value)
        {
            OPLog.Info("Overrides", $"Dry-run enabled. {validationResult.Collection.Overrides.Count} moon tuning entries validated; no runtime moon mutations applied.");
            return;
        }

        _applier.Apply(validationResult.Collection);
    }

    private MoonOverrideReferenceCatalog LoadReferenceCatalog()
    {
        var catalog = new MoonOverrideReferenceCatalog();

        if (StartOfRound.Instance?.levels == null)
        {
            OPLog.Warning("Validation", "Runtime moon catalog is not available.");
        }
        else
        {
            foreach (var level in StartOfRound.Instance.levels)
            {
                if (level != null && !string.IsNullOrWhiteSpace(level.name))
                    catalog.RuntimeMoonIds.Add(level.name);
            }

            OPLog.Info("Validation", $"Loaded {catalog.RuntimeMoonIds.Count} runtime moon IDs for moon .cfg tuning validation.");
        }

        var routeNodes = Resources.FindObjectsOfTypeAll<TerminalNode>();
        if (StartOfRound.Instance?.levels != null && routeNodes != null)
        {
            foreach (var node in routeNodes)
            {
                if (node == null ||
                    node.buyRerouteToMoon < 0 ||
                    node.buyRerouteToMoon >= StartOfRound.Instance.levels.Length)
                {
                    continue;
                }

                var level = StartOfRound.Instance.levels[node.buyRerouteToMoon];
                if (level != null && !string.IsNullOrWhiteSpace(level.name))
                    catalog.RoutePriceMoonIds.Add(level.name);
            }

            OPLog.Info("Validation", $"Resolved {catalog.RoutePriceMoonIds.Count} moon route price IDs from runtime TerminalNode data.");
        }

        return catalog;
    }
}
