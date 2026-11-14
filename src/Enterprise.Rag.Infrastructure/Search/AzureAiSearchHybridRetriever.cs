using System.Globalization;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Enterprise.Rag.Application;
using Enterprise.Rag.Domain.Documents;
using Microsoft.Extensions.Configuration;

namespace Enterprise.Rag.Infrastructure.Search;

public sealed class AzureAiSearchHybridRetriever(
    SearchClient searchClient,
    IEmbeddingGenerator embeddingGenerator,
    IConfiguration configuration) : IHybridRetriever
{
    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        RagSecurityContext securityContext,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(securityContext);

        var embeddings = await embeddingGenerator.GenerateAsync([query], cancellationToken).ConfigureAwait(false);
        if (embeddings.Count != 1)
        {
            throw new InvalidOperationException("The embedding provider must return exactly one vector for a single query.");
        }

        var top = int.TryParse(configuration["AzureSearch:TopK"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var configuredTop) && configuredTop > 0
            ? configuredTop
            : 20;
        var vectorField = configuration["AzureSearch:VectorField"] ?? "contentVector";
        var semanticConfiguration = configuration["AzureSearch:SemanticConfiguration"] ?? "knowledge-semantic";
        var options = new SearchOptions
        {
            Size = top,
            QueryType = SearchQueryType.Semantic,
            SemanticSearch = new SemanticSearchOptions { SemanticConfigurationName = semanticConfiguration },
            Filter = BuildTenantAndGroupFilter(securityContext),
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(embeddings[0])
                    {
                        KNearestNeighborsCount = top,
                        Fields = { vectorField }
                    }
                }
            }
        };
        options.Select.Add("chunkId");
        options.Select.Add("documentId");
        options.Select.Add("tenantId");
        options.Select.Add("fileName");
        options.Select.Add("contentType");
        options.Select.Add("pageNumber");
        options.Select.Add("ordinal");
        options.Select.Add("content");

        var response = await searchClient.SearchAsync<SearchDocument>(query, options, cancellationToken).ConfigureAwait(false);
        var candidates = new List<RetrievedChunk>(top);
        await foreach (var result in response.Value.GetResultsAsync().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var document = result.Document;
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AddMetadata(document, metadata, "fileName");
            AddMetadata(document, metadata, "contentType");
            AddMetadata(document, metadata, "pageNumber");

            var chunk = new DocumentChunk(
                GetValue(document, "chunkId"),
                Guid.Parse(GetValue(document, "documentId")),
                GetValue(document, "tenantId"),
                int.TryParse(GetValue(document, "ordinal"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ordinal) ? ordinal : 0,
                GetValue(document, "content"),
                metadata);
            candidates.Add(new RetrievedChunk(chunk, result.Score ?? 0, null));
        }

        return candidates;
    }

    private static string BuildTenantAndGroupFilter(RagSecurityContext securityContext)
    {
        var tenantId = EscapeODataString(securityContext.TenantId);
        var groups = securityContext.GroupIds
            .Where(groupId => Guid.TryParse(groupId, out _))
            .Select(groupId => groupId.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var groupFilter = groups.Length == 0
            ? "false"
            : $"groupIds/any(g: search.in(g, '{string.Join(',', groups)}', ','))";

        return $"tenantId eq '{tenantId}' and (isTenantWide eq true or {groupFilter})";
    }

    private static string EscapeODataString(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static string GetValue(SearchDocument document, string key)
    {
        return document.TryGetValue(key, out var value) && value is not null
            ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            : string.Empty;
    }

    private static void AddMetadata(SearchDocument document, IDictionary<string, string> metadata, string key)
    {
        var value = GetValue(document, key);
        if (value.Length > 0)
        {
            metadata[key] = value;
        }
    }
}
