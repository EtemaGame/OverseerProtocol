using OverseerProtocol.Core.Validation;
using OverseerProtocol.Data.Models;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class SpawnOverrideValidationResult
{
    public SpawnOverrideCollection Collection { get; }
    public ValidationReport Report { get; }

    public bool CanApply => !Report.HasErrors;

    public SpawnOverrideValidationResult(SpawnOverrideCollection collection, ValidationReport report)
    {
        Collection = collection;
        Report = report;
    }
}
