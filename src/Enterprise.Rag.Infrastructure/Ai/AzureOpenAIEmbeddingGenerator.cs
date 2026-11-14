using Enterprise.Rag.Application;
using Microsoft.SemanticKernel.Embeddings;

namespace Enterprise.Rag.Infrastructure.Ai;

#pragma warning disable CS0618 // The stable Semantic Kernel embedding connector is deprecated but not experimental.
public sealed class AzureOpenAIEmbeddingGenerator(ITextEmbeddingGenerationService embeddingService) : IEmbeddingGenerator
{
    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(texts);
        var embeddings = new List<ReadOnlyMemory<float>>(texts.Count);
        foreach (var text in texts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            embeddings.Add(await embeddingService.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken).ConfigureAwait(false));
        }

        return embeddings;
    }
}
#pragma warning restore CS0618
