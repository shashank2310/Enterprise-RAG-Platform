namespace Enterprise.Rag.Application;

public sealed class RagOrchestrator(
    IHybridRetriever retriever,
    IGroundedAnswerGenerator answerGenerator) : IRagOrchestrator
{
    public async Task<RagAnswer> QueryAsync(
        RagQuery query,
        RagSecurityContext securityContext,
        CancellationToken cancellationToken,
        Func<string, CancellationToken, ValueTask>? onProgress = null,
        Func<string, CancellationToken, ValueTask>? onToken = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Question);
        ArgumentNullException.ThrowIfNull(securityContext);

        if (onProgress is not null)
        {
            await onProgress("Finding relevant passages…", cancellationToken).ConfigureAwait(false);
        }

        var evidence = await retriever.SearchAsync(query.Question, securityContext, cancellationToken).ConfigureAwait(false);
        var selectedEvidence = evidence.Take(8).ToArray();
        if (onProgress is not null)
        {
            var status = selectedEvidence.Length == 0
                ? "No matching passages found. Checking whether the answer is available…"
                : $"Found {selectedEvidence.Length} relevant {(selectedEvidence.Length == 1 ? "passage" : "passages")}. Writing an evidence-based answer…";
            await onProgress(status, cancellationToken).ConfigureAwait(false);
        }

        return await answerGenerator
            .GenerateAsync(query.Question, selectedEvidence, cancellationToken, onToken)
            .ConfigureAwait(false);
    }
}
