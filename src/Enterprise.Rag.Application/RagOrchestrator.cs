namespace Enterprise.Rag.Application;

public sealed class RagOrchestrator(
    IHybridRetriever retriever,
    IGroundedAnswerGenerator answerGenerator) : IRagOrchestrator
{
    public async Task<RagAnswer> QueryAsync(
        RagQuery query,
        RagSecurityContext securityContext,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Question);
        ArgumentNullException.ThrowIfNull(securityContext);

        var evidence = await retriever.SearchAsync(query.Question, securityContext, cancellationToken).ConfigureAwait(false);
        return await answerGenerator
            .GenerateAsync(query.Question, evidence.Take(8).ToArray(), cancellationToken)
            .ConfigureAwait(false);
    }
}
