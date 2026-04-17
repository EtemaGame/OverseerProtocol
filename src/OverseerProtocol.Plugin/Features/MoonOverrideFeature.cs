using System.Collections.Generic;
using System.IO;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models;
using OverseerProtocol.Data.Models.Economy;
using OverseerProtocol.Data.Models.Moons;
using OverseerProtocol.Data.Models.UserConfig;
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
        OPLog.Info("Overrides", $"Loading moon tuning files from: {OPPaths.MoonConfigRoot}");
        var collection = BuildOverrideCollection();

        if (collection.Overrides.Count == 0)
        {
            OPLog.Info("Overrides", "No active moon runtime tuning entries found in moons/*.json.");
            return;
        }

        OPLog.Info("Overrides", $"Moon tuning loaded: schemaVersion={collection.SchemaVersion}, moonTuningCount={collection.Overrides.Count}");

        var references = LoadReferenceCatalog();
        OPLog.Info("Validation", $"Moon validation references: exportedMoons={references.ExportedMoonIds.Count}, runtimeMoons={references.RuntimeMoonIds.Count}, routePriceMoons={references.RoutePriceMoonIds.Count}");
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

    private static MoonOverrideCollection BuildOverrideCollection()
    {
        var collection = new MoonOverrideCollection();
        if (!Directory.Exists(OPPaths.MoonConfigRoot))
            return collection;

        foreach (var path in Directory.GetFiles(OPPaths.MoonConfigRoot, "*.json"))
        {
            var config = JsonFileReader.Read<UserMoonConfigFile>(path);
            if (config == null || string.IsNullOrWhiteSpace(config.MoonId))
                continue;

            if (!config.Enabled)
            {
                OPLog.Info("Overrides", $"Moon tuning disabled for '{config.MoonId}'. Moon runtime fields skipped.");
                continue;
            }

            if (config.MissingFromRuntime)
            {
                OPLog.Warning("Overrides", $"Moon tuning '{config.MoonId}' is marked missingFromRuntime. Moon runtime fields skipped.");
                continue;
            }

            LogReservedMoonFields(config);

            if (config.Override == null ||
                (!config.Override.RoutePrice.HasValue &&
                 !config.Override.RiskLevel.HasValue &&
                 string.IsNullOrWhiteSpace(config.Override.RiskLabel)))
            {
                OPLog.Info("Overrides", $"Moon '{config.MoonId}' has no active supported moon tuning fields.");
                continue;
            }

            collection.Overrides.Add(new MoonOverrideDefinition
            {
                MoonId = config.MoonId,
                RoutePrice = config.Override.RoutePrice,
                RiskLevel = config.Override.RiskLevel,
                RiskLabel = config.Override.RiskLabel
            });
        }

        return collection;
    }

    private static void LogReservedMoonFields(UserMoonConfigFile config)
    {
        if (!string.IsNullOrWhiteSpace(config.Override?.DisplayName) ||
            !string.IsNullOrWhiteSpace(config.Override?.Description))
        {
            OPLog.Warning("Overrides", $"Moon '{config.MoonId}' displayName/description fields are reserved until terminal moon text hooks are verified.");
        }

        if (config.Scrap?.MinScrapCount != null ||
            config.Scrap?.MaxScrapCount != null ||
            config.Scrap?.MinTotalScrapValue != null ||
            config.Scrap?.MaxTotalScrapValue != null ||
            config.Scrap?.ScrapAmountMultiplier != null ||
            config.Scrap?.ScrapValueMultiplier != null)
        {
            OPLog.Warning("Overrides", $"Moon '{config.MoonId}' scrap fields are reserved until round scrap generation hooks are verified.");
        }

        if (config.Items != null &&
            (!string.Equals(config.Items.Mode, "keep", System.StringComparison.OrdinalIgnoreCase) ||
             config.Items.Entries.Count > 0))
        {
            OPLog.Warning("Overrides", $"Moon '{config.MoonId}' item spawn pool fields are reserved until moon scrap item table hooks are verified.");
        }
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

            OPLog.Info("Validation", $"Loaded {catalog.ExportedMoonIds.Count} exported moon IDs for moon tuning validation.");
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

            OPLog.Info("Validation", $"Loaded {catalog.RuntimeMoonIds.Count} runtime moon IDs for moon tuning validation.");
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

            OPLog.Info("Validation", $"Loaded {catalog.RoutePriceMoonIds.Count} moon route price IDs for moon tuning validation.");
        }

        return catalog;
    }
}
