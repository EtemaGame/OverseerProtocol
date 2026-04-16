using System.Collections.Generic;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models;
using OverseerProtocol.Data.Models.Economy;
using OverseerProtocol.Data.Models.Moons;
using OverseerProtocol.GameAbstractions.Overrides;

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
        var path = OPPaths.GetMoonOverridePath(presetName);

        OPLog.Info("Overrides", $"Loading moon overrides from: {path}");

        var collection = JsonFileReader.Read<MoonOverrideCollection>(path);

        if (collection == null)
        {
            OPLog.Info("Overrides", "No valid moons.override.json found or file is empty.");
            return;
        }

        var references = LoadReferenceCatalog();
        var validationResult = _validator.Validate(collection, references);
        validationResult.Report.WriteToLog("Validation");

        if (!validationResult.CanApplyWithStrictMode(OPConfig.StrictValidation.Value))
        {
            var reason = OPConfig.StrictValidation.Value && validationResult.Report.WarningCount > 0
                ? "strict validation is enabled and warnings were reported"
                : "critical validation errors were reported";

            OPLog.Warning("Validation", $"Moon override collection was not applied because {reason}.");
            return;
        }

        if (OPConfig.DryRunOverrides.Value)
        {
            OPLog.Info("Overrides", $"Dry-run enabled. {validationResult.Collection.Overrides.Count} moon overrides validated; no runtime moon mutations applied.");
            return;
        }

        _applier.Apply(validationResult.Collection);
    }

    private MoonOverrideReferenceCatalog LoadReferenceCatalog()
    {
        var catalog = new MoonOverrideReferenceCatalog();

        var moons = JsonFileReader.Read<List<MoonDefinition>>(OPPaths.MoonExportPath);
        if (moons == null || moons.Count == 0)
        {
            OPLog.Warning("Validation", $"No exported moon catalog found at {OPPaths.MoonExportPath}.");
        }
        else
        {
            foreach (var moon in moons)
            {
                if (moon != null && !string.IsNullOrWhiteSpace(moon.Id))
                    catalog.ExportedMoonIds.Add(moon.Id);
            }

            OPLog.Info("Validation", $"Loaded {catalog.ExportedMoonIds.Count} exported moon IDs for moon override validation.");
        }

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

            OPLog.Info("Validation", $"Loaded {catalog.RuntimeMoonIds.Count} runtime moon IDs for moon override validation.");
        }

        var economyProfiles = JsonFileReader.Read<List<MoonEconomyProfile>>(OPPaths.MoonEconomyExportPath);
        if (economyProfiles == null || economyProfiles.Count == 0)
        {
            OPLog.Warning("Validation", $"No exported moon economy catalog found at {OPPaths.MoonEconomyExportPath}.");
        }
        else
        {
            foreach (var profile in economyProfiles)
            {
                if (profile != null && profile.HasRouteNode && !string.IsNullOrWhiteSpace(profile.MoonId))
                    catalog.RoutePriceMoonIds.Add(profile.MoonId);
            }

            OPLog.Info("Validation", $"Loaded {catalog.RoutePriceMoonIds.Count} moon route price IDs for moon override validation.");
        }

        return catalog;
    }
}
