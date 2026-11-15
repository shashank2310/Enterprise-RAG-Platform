using System.Net.Http.Json;
using System.Text.Json;
using Enterprise.Rag.Application;
using Microsoft.Extensions.Configuration;

namespace Enterprise.Rag.Infrastructure.Ai;

public sealed class OllamaEmbeddingGenerator(HttpClient httpClient, IConfiguration configuration) : IEmbeddingGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string model = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(texts);
        if (texts.Count == 0)
        {
            return [];
        }

        var embeddings = new List<ReadOnlyMemory<float>>(texts.Count);
        foreach (var batch in texts.Chunk(32))
        {
            using var response = await httpClient.PostAsJsonAsync(
                "api/embed",
                new EmbedRequest(model, batch),
                JsonOptions,
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("Ollama returned an empty embedding response.");
            if (result.Embeddings.Length != batch.Length || result.Embeddings.Any(vector => vector.Length == 0))
            {
                throw new InvalidOperationException("Ollama returned a different number of embeddings than requested, or an empty vector.");
            }

            embeddings.AddRange(result.Embeddings.Select(vector => (ReadOnlyMemory<float>)vector));
        }

        return embeddings;
    }

    private sealed record EmbedRequest(string Model, IReadOnlyList<string> Input);
    private sealed record EmbedResponse(float[][] Embeddings);
}
