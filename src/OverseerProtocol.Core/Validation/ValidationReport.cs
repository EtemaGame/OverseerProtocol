using System.Collections.Generic;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.Core.Validation;

public sealed class ValidationReport
{
    private readonly List<ValidationIssue> _issues = new();

    public IReadOnlyList<ValidationIssue> Issues => _issues;
    public int InfoCount { get; private set; }
    public int WarningCount { get; private set; }
    public int ErrorCount { get; private set; }
    public bool HasErrors => ErrorCount > 0;

    public void Info(string code, string message, string path = "") =>
        Add(new ValidationIssue(ValidationSeverity.Info, code, message, path));

    public void Warning(string code, string message, string path = "") =>
        Add(new ValidationIssue(ValidationSeverity.Warning, code, message, path));

    public void Error(string code, string message, string path = "") =>
        Add(new ValidationIssue(ValidationSeverity.Error, code, message, path));

    public void WriteToLog(string category = "Validation")
    {
        foreach (var issue in _issues)
        {
            var message = string.IsNullOrWhiteSpace(issue.Path)
                ? $"{issue.Code}: {issue.Message}"
                : $"{issue.Code} at {issue.Path}: {issue.Message}";

            switch (issue.Severity)
            {
                case ValidationSeverity.Info:
                    OPLog.Info(category, message);
                    break;
                case ValidationSeverity.Warning:
                    OPLog.Warning(category, message);
                    break;
                case ValidationSeverity.Error:
                    OPLog.Error(category, message);
                    break;
            }
        }

        OPLog.Info(category, $"Validation completed with {ErrorCount} errors, {WarningCount} warnings, {InfoCount} info messages.");
    }

    private void Add(ValidationIssue issue)
    {
        _issues.Add(issue);

        switch (issue.Severity)
        {
            case ValidationSeverity.Info:
                InfoCount++;
                break;
            case ValidationSeverity.Warning:
                WarningCount++;
                break;
            case ValidationSeverity.Error:
                ErrorCount++;
                break;
        }
    }
}
