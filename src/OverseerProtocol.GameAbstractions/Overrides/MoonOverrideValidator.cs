using System;
using System.Collections.Generic;
using OverseerProtocol.Core.Validation;
using OverseerProtocol.Data.Models;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class MoonOverrideValidator
{
    public const int MinRiskLevel = 0;
    public const int MaxRiskLevel = 5;

    public MoonOverrideValidationResult Validate(MoonOverrideCollection collection, MoonOverrideReferenceCatalog references)
    {
        var report = new ValidationReport();
        var validatedCollection = new MoonOverrideCollection();

        if (!references.HasExportedMoonCatalog)
            report.Error("MOON_CATALOG_MISSING", "Moon overrides require a valid exported moon catalog.");

        if (!references.HasRuntimeMoonCatalog)
            report.Error("MOON_RUNTIME_CATALOG_MISSING", "Moon overrides require the runtime moon catalog to be loaded.");

        if (report.HasErrors)
            return new MoonOverrideValidationResult(validatedCollection, report);

        if (collection?.Overrides == null || collection.Overrides.Count == 0)
        {
            report.Info("MOON_OVERRIDES_EMPTY", "No moon override entries were found.");
            return new MoonOverrideValidationResult(validatedCollection, report);
        }

        var observedMoons = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < collection.Overrides.Count; i++)
        {
            var moonOverride = collection.Overrides[i];
            var moonPath = $"overrides[{i}]";

            if (moonOverride == null)
            {
                report.Warning("MOON_OVERRIDE_NULL", "Found null moon override entry. Skipping entry.", moonPath);
                continue;
            }

            if (string.IsNullOrWhiteSpace(moonOverride.MoonId))
            {
                report.Warning("MOON_ID_EMPTY", "Found moon override entry with empty MoonId. Skipping entry.", $"{moonPath}.moonId");
                continue;
            }

            if (!references.ExportedMoonIds.Contains(moonOverride.MoonId))
            {
                report.Warning("MOON_ID_UNKNOWN", $"MoonId '{moonOverride.MoonId}' does not exist in the exported moon catalog. Skipping moon override.", $"{moonPath}.moonId");
                continue;
            }

            if (!references.RuntimeMoonIds.Contains(moonOverride.MoonId))
            {
                report.Warning("MOON_ID_UNRESOLVED", $"MoonId '{moonOverride.MoonId}' does not exist in the runtime moon catalog. Skipping moon override.", $"{moonPath}.moonId");
                continue;
            }

            if (!observedMoons.Add(moonOverride.MoonId))
            {
                report.Warning("MOON_ID_DUPLICATE", $"Duplicate MoonId '{moonOverride.MoonId}' found. First entry wins; duplicate skipped.", $"{moonPath}.moonId");
                continue;
            }

            var validatedOverride = new MoonOverrideDefinition
            {
                MoonId = moonOverride.MoonId,
                RiskLevel = ValidateRiskLevel(moonOverride.RiskLevel, report, $"{moonPath}.riskLevel", moonOverride.MoonId),
                RiskLabel = ValidateRiskLabel(moonOverride.RiskLabel, report, $"{moonPath}.riskLabel", moonOverride.MoonId),
                RoutePrice = ValidateRoutePrice(moonOverride.RoutePrice, references, report, $"{moonPath}.routePrice", moonOverride.MoonId)
            };

            if (!validatedOverride.RiskLevel.HasValue &&
                string.IsNullOrWhiteSpace(validatedOverride.RiskLabel) &&
                !validatedOverride.RoutePrice.HasValue)
            {
                report.Warning("MOON_OVERRIDE_EMPTY", $"Moon override for '{moonOverride.MoonId}' does not define any supported fields. Skipping moon override.", moonPath);
                continue;
            }

            validatedCollection.Overrides.Add(validatedOverride);
        }

        return new MoonOverrideValidationResult(validatedCollection, report);
    }

    private static int? ValidateRiskLevel(int? riskLevel, ValidationReport report, string path, string moonId)
    {
        if (!riskLevel.HasValue)
            return null;

        if (riskLevel.Value < MinRiskLevel)
        {
            report.Warning("MOON_RISK_LEVEL_CLAMPED", $"Moon '{moonId}' riskLevel {riskLevel.Value} is below {MinRiskLevel}. Clamped to {MinRiskLevel}.", path);
            return MinRiskLevel;
        }

        if (riskLevel.Value > MaxRiskLevel)
        {
            report.Warning("MOON_RISK_LEVEL_CLAMPED", $"Moon '{moonId}' riskLevel {riskLevel.Value} is above {MaxRiskLevel}. Clamped to {MaxRiskLevel}.", path);
            return MaxRiskLevel;
        }

        return riskLevel.Value;
    }

    private static string? ValidateRiskLabel(string? riskLabel, ValidationReport report, string path, string moonId)
    {
        if (riskLabel == null)
            return null;

        var trimmed = riskLabel.Trim();
        if (trimmed.Length == 0)
        {
            report.Warning("MOON_RISK_LABEL_EMPTY", $"Moon '{moonId}' riskLabel is empty. Field skipped.", path);
            return null;
        }

        if (trimmed.Length > 32)
        {
            report.Warning("MOON_RISK_LABEL_LONG", $"Moon '{moonId}' riskLabel is longer than 32 characters. Field skipped.", path);
            return null;
        }

        return trimmed;
    }

    private static int? ValidateRoutePrice(int? routePrice, MoonOverrideReferenceCatalog references, ValidationReport report, string path, string moonId)
    {
        if (!routePrice.HasValue)
            return null;

        if (routePrice.Value < 0)
        {
            report.Warning("MOON_ROUTE_PRICE_CLAMPED", $"Moon '{moonId}' routePrice {routePrice.Value} is below 0. Clamped to 0.", path);
            routePrice = 0;
        }

        if (!references.RoutePriceMoonIds.Contains(moonId))
            report.Warning("MOON_ROUTE_PRICE_NODE_MISSING", $"Moon '{moonId}' has no exported route node. Runtime route price override may not find a terminal node.", path);

        return routePrice.Value;
    }
}
