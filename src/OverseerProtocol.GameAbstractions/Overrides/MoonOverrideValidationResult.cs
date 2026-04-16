using OverseerProtocol.Core.Validation;
using OverseerProtocol.Data.Models;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class MoonOverrideValidationResult
{
    public MoonOverrideCollection Collection { get; }
    public ValidationReport Report { get; }

    public bool CanApply => !Report.HasErrors;

    public MoonOverrideValidationResult(MoonOverrideCollection collection, ValidationReport report)
    {
        Collection = collection;
        Report = report;
    }

    public bool CanApplyWithStrictMode(bool strictValidation) =>
        !Report.HasErrors && (!strictValidation || Report.WarningCount == 0);
}
