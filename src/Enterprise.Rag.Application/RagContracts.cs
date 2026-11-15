using Enterprise.Rag.Domain.Documents;

namespace Enterprise.Rag.Application;

public sealed record RagQuery(string Question);

public sealed record RagSecurityContext(
    string TenantId,
    string SubjectId,
    IReadOnlySet<string> GroupIds);

public sealed record RetrievedChunk(DocumentChunk Chunk, double SearchScore, double? RerankerScore);

public sealed record AnswerCitation(string DocumentId, string ChunkId, string FileName, string? PageNumber);

public sealed record RagAnswer(string Answer, IReadOnlyList<AnswerCitation> Citations, double Confidence);

public sealed record EvaluationMetrics(
    double Faithfulness,
    double AnswerRelevancy,
    double ContextPrecision,
    double ContextRecall,
    bool HallucinationDetected);

public sealed record ReadinessResult(bool IsReady, IReadOnlyList<string> MissingComponents);

public interface IRagReadiness
{
    Task<ReadinessResult> CheckAsync(CancellationToken cancellationToken);
}

public interface IRagOrchestrator
{
    Task<RagAnswer> QueryAsync(
        RagQuery query,
        RagSecurityContext securityContext,
        CancellationToken cancellationToken,
        Func<string, CancellationToken, ValueTask>? onProgress = null,
        Func<string, CancellationToken, ValueTask>? onToken = null);
}

public interface IDocumentTextExtractor
{
    Task<string> ExtractAsync(Stream content, string fileName, CancellationToken cancellationToken);
}

public interface IDocumentChunker
{
    IReadOnlyList<DocumentChunk> Chunk(KnowledgeDocument document, string extractedText, int targetCharacters, int overlapCharacters);
}

public interface ISemanticDocumentChunker : IDocumentChunker
{
}

public interface IDocumentMetadataEnricher
{
    IReadOnlyDictionary<string, string> Enrich(KnowledgeDocument document, IReadOnlyDictionary<string, string> extractedMetadata);
}

public interface IEmbeddingGenerator
{
    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken);
}

public sealed record DocumentIndexResult(Guid DocumentId, int ChunkCount, string FileName);

public interface IDocumentIngestionService
{
    Task<DocumentIndexResult> IndexAsync(
        Stream content,
        string fileName,
        string contentType,
        RagSecurityContext securityContext,
        CancellationToken cancellationToken);
}

public interface IDocumentIndexStore
{
    Task<Guid> SaveAsync(
        KnowledgeDocument document,
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<ReadOnlyMemory<float>> embeddings,
        CancellationToken cancellationToken);
}

public interface IHybridRetriever
{
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(string query, RagSecurityContext securityContext, CancellationToken cancellationToken);
}

public interface IQueryEnhancer
{
    Task<IReadOnlyList<string>> RewriteAndExpandAsync(string query, CancellationToken cancellationToken);
}

public interface IContextReranker
{
    Task<IReadOnlyList<RetrievedChunk>> RerankAsync(string query, IReadOnlyList<RetrievedChunk> candidates, CancellationToken cancellationToken);
}

public interface IContextFilter
{
    IReadOnlyList<RetrievedChunk> Filter(string query, IReadOnlyList<RetrievedChunk> candidates);
}

public interface IGroundedAnswerGenerator
{
    Task<RagAnswer> GenerateAsync(
        string query,
        IReadOnlyList<RetrievedChunk> context,
        CancellationToken cancellationToken,
        Func<string, CancellationToken, ValueTask>? onToken = null);
}

public interface IRagEvaluator
{
    Task<EvaluationMetrics> EvaluateAsync(
        string query,
        RagAnswer answer,
        IReadOnlyList<RetrievedChunk> context,
        string? referenceAnswer,
        CancellationToken cancellationToken);
}

public sealed class RagNotConfiguredException(string message) : Exception(message)
{
}
