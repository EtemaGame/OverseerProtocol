using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.GameAbstractions.Overrides;

namespace OverseerProtocol.Features;

public sealed class ItemOverrideFeature
{
    private readonly ItemOverrideValidator _validator;
    private readonly ItemOverrideApplier _applier;

    public ItemOverrideFeature()
    {
        _validator = new ItemOverrideValidator();
        _applier = new ItemOverrideApplier();
    }

    public void ApplyOverrides(string presetName)
    {
        OPLog.Info("Overrides", "Loading item tuning from BepInEx .cfg.");
        var collection = new RuntimeTuningConfig(OPConfig.ConfigFile).BuildItemOverrides();
        OPLog.Info("Overrides", $"Item .cfg tuning loaded: activeTuningCount={collection.Overrides.Count}");

        var references = LoadReferenceCatalog();
        OPLog.Info("Validation", $"Item validation references: runtimeIds={references.RuntimeItemIds.Count}");
        var validationResult = _validator.Validate(collection, references);
        validationResult.Report.WriteToLog("Validation");

        var abortOnWarnings = OPConfig.StrictValidation.Value || OPConfig.AbortOnInvalidOverrideBlock.Value;
        OPLog.Info("Validation", $"Item validation policy: strict={OPConfig.StrictValidation.Value}, abortOnInvalidBlock={OPConfig.AbortOnInvalidOverrideBlock.Value}, abortOnWarnings={abortOnWarnings}, canApply={validationResult.CanApplyWithStrictMode(abortOnWarnings)}");
        if (!validationResult.CanApplyWithStrictMode(abortOnWarnings))
        {
            var reason = abortOnWarnings && validationResult.Report.WarningCount > 0
                ? "strict tuning validation is enabled and warnings were reported"
                : "critical validation errors were reported";

            OPLog.Warning("Validation", $"Item tuning was not applied because {reason}.");
            return;
        }

        if (OPConfig.DryRunOverrides.Value)
        {
            OPLog.Info("Overrides", $"Dry-run enabled. {validationResult.Collection.Overrides.Count} item tuning entries validated; no runtime item mutations applied.");
            return;
        }

        _applier.Apply(validationResult.Collection);
    }

    private ItemOverrideReferenceCatalog LoadReferenceCatalog()
    {
        var catalog = new ItemOverrideReferenceCatalog();

        if (StartOfRound.Instance?.allItemsList?.itemsList == null)
        {
            OPLog.Warning("Validation", "Runtime item catalog is not available.");
        }
        else
        {
            foreach (var item in StartOfRound.Instance.allItemsList.itemsList)
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.name))
                    catalog.RuntimeItemIds.Add(item.name);
            }

            OPLog.Info("Validation", $"Loaded {catalog.RuntimeItemIds.Count} runtime item IDs for item .cfg tuning validation.");
        }

        return catalog;
    }
}
