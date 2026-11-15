using System.Text.Json;
using Enterprise.Rag.Application;
using Enterprise.Rag.Domain.Documents;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Enterprise.Rag.Infrastructure.Search;

public sealed class PostgresVectorRetriever(
    NpgsqlDataSource dataSource,
    IEmbeddingGenerator embeddingGenerator,
    IConfiguration configuration) : IHybridRetriever
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly int topK = int.TryParse(configuration["Ollama:TopK"], out var configuredTopK) && configuredTopK > 0
        ? Math.Min(configuredTopK, 20)
        : 8;

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        RagSecurityContext securityContext,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(securityContext);

        var queryEmbeddings = await embeddingGenerator.GenerateAsync([query], cancellationToken).ConfigureAwait(false);
        if (queryEmbeddings.Count != 1)
        {
            throw new InvalidOperationException("The embedding provider must return exactly one vector for a single query.");
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await SetTenantAsync(connection, transaction, securityContext.TenantId, cancellationToken).ConfigureAwait(false);

        await using var command = new NpgsqlCommand("""
            SELECT c.id, c.document_id, c.tenant_id, c.ordinal, c.content, c.metadata::text, c.embedding::text
            FROM document_chunks AS c
            INNER JOIN documents AS d ON d.id = c.document_id AND d.tenant_id = c.tenant_id
            WHERE c.tenant_id = @tenantId AND d.status = 'Indexed' AND c.embedding IS NOT NULL
            """, connection, transaction);
        command.Parameters.AddWithValue("tenantId", securityContext.TenantId);

        var candidates = new List<RetrievedChunk>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var storedEmbedding = JsonSerializer.Deserialize<float[]>(reader.GetString(6), JsonOptions)
                    ?? throw new InvalidOperationException("A stored document embedding could not be read.");
                var score = CosineSimilarity(queryEmbeddings[0].Span, storedEmbedding);
                var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(5), JsonOptions)
                    ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var chunk = new DocumentChunk(
                    reader.GetString(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetString(4),
                    metadata);
                candidates.Add(new RetrievedChunk(chunk, score, null));
            }
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return candidates
            .OrderByDescending(candidate => candidate.SearchScore)
            .Take(topK)
            .ToArray();
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

    private static double CosineSimilarity(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
        if (left.Length != right.Length)
        {
            throw new InvalidOperationException("Stored and query embedding dimensions differ. Re-index documents with the configured embedding model.");
        }

        double dot = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;
        for (var index = 0; index < left.Length; index++)
        {
            dot += left[index] * right[index];
            leftMagnitude += left[index] * left[index];
            rightMagnitude += right[index] * right[index];
        }

        return leftMagnitude == 0 || rightMagnitude == 0
            ? 0
            : dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}
