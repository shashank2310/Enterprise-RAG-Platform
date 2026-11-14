using Enterprise.Rag.Application;

namespace Enterprise.Rag.Infrastructure;

public sealed class RagReadiness : IRagReadiness
{
    public ReadinessResult Check() => new(
        false,
        ["The end-to-end RAG orchestrator, tenant-scoped persistence, and index lifecycle are not enabled in this starter."]);
}
