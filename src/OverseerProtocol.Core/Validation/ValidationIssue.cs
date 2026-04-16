namespace OverseerProtocol.Core.Validation;

public sealed class ValidationIssue
{
    public ValidationSeverity Severity { get; }
    public string Code { get; }
    public string Message { get; }
    public string Path { get; }

    public ValidationIssue(ValidationSeverity severity, string code, string message, string path = "")
    {
        Severity = severity;
        Code = code;
        Message = message;
        Path = path;
    }
}
