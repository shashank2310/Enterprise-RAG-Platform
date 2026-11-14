namespace Enterprise.Rag.Domain.Documents;

public enum DocumentStatus
{
    Pending,
    Processing,
    Indexed,
    Failed,
    Deleted
}

public sealed record KnowledgeDocument(
    Guid Id,
    string TenantId,
    string FileName,
    string ContentType,
    string Sha256,
    string BlobUri,
    DocumentStatus Status,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record DocumentChunk(
    string Id,
    Guid DocumentId,
    string TenantId,
    int Ordinal,
    string Text,
    IReadOnlyDictionary<string, string> Metadata);
