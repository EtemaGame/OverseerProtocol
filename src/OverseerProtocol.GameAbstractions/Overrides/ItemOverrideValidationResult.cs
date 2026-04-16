using OverseerProtocol.Core.Validation;
using OverseerProtocol.Data.Models;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class ItemOverrideValidationResult
{
    public ItemOverrideCollection Collection { get; }
    public ValidationReport Report { get; }

    public bool CanApply => !Report.HasErrors;

    public ItemOverrideValidationResult(ItemOverrideCollection collection, ValidationReport report)
    {
        Collection = collection;
        Report = report;
    }

    public bool CanApplyWithStrictMode(bool strictValidation) =>
        !Report.HasErrors && (!strictValidation || Report.WarningCount == 0);
}
