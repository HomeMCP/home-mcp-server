namespace HomeMcp.Application.Orchestration;

public interface IChatOrchestrator
{
    IAsyncEnumerable<TurnEvent> OrchestrateAsync(
        OrchestratorContext context,
        CancellationToken ct = default);
}
