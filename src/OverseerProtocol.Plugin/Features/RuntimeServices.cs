using OverseerProtocol.GameAbstractions.State;

namespace OverseerProtocol.Features;

public static class RuntimeServices
{
    private static readonly RuntimeStateSnapshot Snapshot = new();

    public static RuntimeOrchestrator Orchestrator { get; } = new(Snapshot);
}
