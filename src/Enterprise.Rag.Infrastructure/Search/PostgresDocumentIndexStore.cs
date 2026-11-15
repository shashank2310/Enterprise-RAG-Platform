using System.Text.Json;
using Enterprise.Rag.Application;
using Enterprise.Rag.Domain.Documents;
using Npgsql;

namespace Enterprise.Rag.Infrastructure.Search;

public sealed class PostgresDocumentIndexStore(NpgsqlDataSource dataSource) : IDocumentIndexStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Guid> SaveAsync(
        KnowledgeDocument document,
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<ReadOnlyMemory<float>> embeddings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (chunks.Count != embeddings.Count)
        {
            throw new ArgumentException("Each document chunk must have one embedding.", nameof(embeddings));
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await SetTenantAsync(connection, transaction, document.TenantId, cancellationToken).ConfigureAwait(false);

        Guid storedDocumentId;
        await using (var command = new NpgsqlCommand("""
            INSERT INTO documents (id, tenant_id, file_name, content_type, sha256, blob_uri, status, created_by)
            VALUES (@id, @tenantId, @fileName, @contentType, @sha256, @blobUri, 'Indexed', @createdBy)
            ON CONFLICT (tenant_id, sha256) DO UPDATE SET
                file_name = EXCLUDED.file_name,
                content_type = EXCLUDED.content_type,
                blob_uri = EXCLUDED.blob_uri,
                status = 'Indexed',
                updated_at = now()
            RETURNING id
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("id", document.Id);
            command.Parameters.AddWithValue("tenantId", document.TenantId);
            command.Parameters.AddWithValue("fileName", document.FileName);
            command.Parameters.AddWithValue("contentType", document.ContentType);
            command.Parameters.AddWithValue("sha256", document.Sha256);
            command.Parameters.AddWithValue("blobUri", document.BlobUri);
            command.Parameters.AddWithValue("createdBy", document.CreatedBy);
            storedDocumentId = (Guid)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The document index operation did not return a document ID."));
        }

        await using (var delete = new NpgsqlCommand(
            "DELETE FROM document_chunks WHERE tenant_id = @tenantId AND document_id = @documentId",
            connection,
            transaction))
        {
            delete.Parameters.AddWithValue("tenantId", document.TenantId);
            delete.Parameters.AddWithValue("documentId", storedDocumentId);
            await delete.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = chunks[index];
            var metadata = JsonSerializer.Serialize(chunk.Metadata, JsonOptions);
            var embedding = JsonSerializer.Serialize(embeddings[index].ToArray(), JsonOptions);
            await using var insert = new NpgsqlCommand("""
                INSERT INTO document_chunks (id, tenant_id, document_id, ordinal, content, metadata, embedding)
                VALUES (@id, @tenantId, @documentId, @ordinal, @content, @metadata::jsonb, @embedding::jsonb)
                """, connection, transaction);
            insert.Parameters.AddWithValue("id", chunk.Id);
            insert.Parameters.AddWithValue("tenantId", document.TenantId);
            insert.Parameters.AddWithValue("documentId", storedDocumentId);
            insert.Parameters.AddWithValue("ordinal", chunk.Ordinal);
            insert.Parameters.AddWithValue("content", chunk.Text);
            insert.Parameters.AddWithValue("metadata", metadata);
            insert.Parameters.AddWithValue("embedding", embedding);
            await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return storedDocumentId;
    }

    private static async Task SetTenantAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string tenantId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT set_config('app.tenant_id', @tenantId, true)", connection, transaction);
        command.Parameters.AddWithValue("tenantId", tenantId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
