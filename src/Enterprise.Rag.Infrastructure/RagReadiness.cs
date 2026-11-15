using System.Net.Http.Json;
using Enterprise.Rag.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Npgsql;

namespace Enterprise.Rag.Infrastructure;

public sealed class RagReadiness(IServiceProvider serviceProvider, IConfiguration configuration) : IRagReadiness, IDisposable
{
    private readonly HttpClient ollama = CreateOllamaClient(configuration);

    public async Task<ReadinessResult> CheckAsync(CancellationToken cancellationToken)
    {
        var missing = new List<string>();
        if (!string.Equals(configuration["Rag:Provider"], "Ollama", StringComparison.OrdinalIgnoreCase)
            || !bool.TryParse(configuration["Rag:EnableLocalPostgresIndex"], out var localIndexEnabled)
            || !localIndexEnabled)
        {
            return new ReadinessResult(false, ["The configured RAG provider and local PostgreSQL index are not enabled."]);
        }

        var dataSource = serviceProvider.GetService<NpgsqlDataSource>();
        if (dataSource is null)
        {
            missing.Add("PostgreSQL connection is not configured.");
        }
        else
        {
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (NpgsqlException)
            {
                missing.Add("PostgreSQL is unavailable.");
            }
            catch (TimeoutException)
            {
                missing.Add("PostgreSQL connection timed out.");
            }
        }

        var chatModel = configuration["Ollama:ChatModel"] ?? "qwen3:8b";
        var embeddingModel = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
        foreach (var model in new[] { chatModel, embeddingModel }.Distinct(StringComparer.Ordinal))
        {
            try
            {
                using var response = await ollama.PostAsJsonAsync("api/show", new { name = model }, cancellationToken)
                    .ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    missing.Add($"Ollama model '{model}' is unavailable (HTTP {(int)response.StatusCode}).");
                }
            }
            catch (HttpRequestException)
            {
                missing.Add("Ollama is unreachable at the configured base URL.");
                break;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                missing.Add("Ollama readiness request timed out.");
                break;
            }
        }

        return new ReadinessResult(missing.Count == 0, missing);
    }

    public void Dispose() => ollama.Dispose();

    private static HttpClient CreateOllamaClient(IConfiguration configuration)
    {
        var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var normalized = baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : $"{baseUrl}/";
        return new HttpClient { BaseAddress = new Uri(normalized, UriKind.Absolute), Timeout = TimeSpan.FromSeconds(5) };
    }
}
