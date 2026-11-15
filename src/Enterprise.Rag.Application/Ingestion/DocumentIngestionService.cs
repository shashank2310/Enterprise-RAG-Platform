using System.Security.Cryptography;
using Enterprise.Rag.Domain.Documents;

namespace Enterprise.Rag.Application.Ingestion;

public sealed class DocumentIngestionService(
    IDocumentTextExtractor textExtractor,
    IDocumentChunker chunker,
    IEmbeddingGenerator embeddingGenerator,
    IDocumentIndexStore indexStore) : IDocumentIngestionService
{
    private const int MaximumDocumentBytes = 20 * 1024 * 1024;
    private const int TargetChunkCharacters = 1_200;
    private const int OverlapCharacters = 180;
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".html", ".htm", ".docx", ".pdf"
    };

    public async Task<DocumentIndexResult> IndexAsync(
        Stream content,
        string fileName,
        string contentType,
        RagSecurityContext securityContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(securityContext);

        var safeFileName = Path.GetFileName(fileName);
        if (!SupportedExtensions.Contains(Path.GetExtension(safeFileName)))
        {
            throw new NotSupportedException("Supported document types are PDF, DOCX, TXT, HTML, and HTM.");
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (buffer.Length == 0)
        {
            throw new InvalidDataException("The uploaded document is empty.");
        }

        if (buffer.Length > MaximumDocumentBytes)
        {
            throw new InvalidDataException($"The document exceeds the {MaximumDocumentBytes / (1024 * 1024)} MB upload limit.");
        }

        buffer.Position = 0;
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(buffer, cancellationToken).ConfigureAwait(false)).ToLowerInvariant();
        buffer.Position = 0;
        var extractedText = await textExtractor.ExtractAsync(buffer, safeFileName, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            throw new InvalidDataException("No text could be extracted from the uploaded document.");
        }

        var documentId = Guid.NewGuid();
        var document = new KnowledgeDocument(
            documentId,
            securityContext.TenantId,
            safeFileName,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            hash,
            $"local://{documentId:D}",
            DocumentStatus.Indexed,
            DateTimeOffset.UtcNow,
            securityContext.SubjectId);
        var chunks = chunker.Chunk(document, extractedText, TargetChunkCharacters, OverlapCharacters);
        if (chunks.Count == 0)
        {
            throw new InvalidDataException("The document did not produce any indexable text chunks.");
        }

        var embeddings = await embeddingGenerator.GenerateAsync(chunks.Select(chunk => chunk.Text).ToArray(), cancellationToken)
            .ConfigureAwait(false);
        if (embeddings.Count != chunks.Count)
        {
            throw new InvalidOperationException("The embedding provider returned a different number of vectors than document chunks.");
        }

        var storedDocumentId = await indexStore.SaveAsync(document, chunks, embeddings, cancellationToken).ConfigureAwait(false);
        return new DocumentIndexResult(storedDocumentId, chunks.Count, safeFileName);
    }
}
