using Azure.AI.DocumentIntelligence;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
using Enterprise.Rag.Application;
using Enterprise.Rag.Application.Ingestion;
using Enterprise.Rag.Infrastructure.Ai;
using Enterprise.Rag.Infrastructure.Ingestion;
using Enterprise.Rag.Infrastructure.Search;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Enterprise.Rag.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRagInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IRagReadiness, RagReadiness>();
        services.AddSingleton<IDocumentChunker, RecursiveTextChunker>();
        services.AddSingleton<IDocumentTextExtractor>(serviceProvider =>
            new DocumentTextExtractor(serviceProvider.GetService<DocumentIntelligenceClient>()));

        var ragProvider = configuration["Rag:Provider"] ?? "Ollama";
        var ollamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var useLocalPostgresIndex = bool.TryParse(configuration["Rag:EnableLocalPostgresIndex"], out var localIndexEnabled) && localIndexEnabled;
        var postgresConnectionString = configuration.GetConnectionString("Postgres");

        if (string.Equals(ragProvider, "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            var ollamaUri = GetServiceBaseUri(ollamaBaseUrl);
            services.AddHttpClient<IEmbeddingGenerator, OllamaEmbeddingGenerator>(client => client.BaseAddress = ollamaUri);
            services.AddHttpClient<IGroundedAnswerGenerator, OllamaGroundedAnswerGenerator>(client =>
            {
                client.BaseAddress = ollamaUri;
                client.Timeout = TimeSpan.FromMinutes(3);
            });
        }

        var azureOpenAiEndpoint = configuration["AzureOpenAI:Endpoint"];
        var chatDeployment = configuration["AzureOpenAI:ChatDeployment"];
        var embeddingDeployment = configuration["AzureOpenAI:EmbeddingDeployment"];

        if (string.Equals(ragProvider, "AzureOpenAI", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(azureOpenAiEndpoint)
            && !string.IsNullOrWhiteSpace(chatDeployment)
            && !string.IsNullOrWhiteSpace(embeddingDeployment))
        {
            var credential = new DefaultAzureCredential();
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddAzureOpenAIChatCompletion(chatDeployment, azureOpenAiEndpoint, credential);
#pragma warning disable CS0618 // The replacement embedding connector is currently marked experimental.
            kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(embeddingDeployment, azureOpenAiEndpoint, credential);
#pragma warning restore CS0618
            var kernel = kernelBuilder.Build();

            services.AddSingleton(kernel);
            services.AddSingleton(kernel.GetRequiredService<IChatCompletionService>());
#pragma warning disable CS0618
            services.AddSingleton(kernel.GetRequiredService<Microsoft.SemanticKernel.Embeddings.ITextEmbeddingGenerationService>());
#pragma warning restore CS0618
            services.AddSingleton<IEmbeddingGenerator, AzureOpenAIEmbeddingGenerator>();
            services.AddSingleton<IGroundedAnswerGenerator, SemanticKernelGroundedAnswerGenerator>();
        }

        var searchEndpoint = configuration["AzureSearch:Endpoint"];
        var searchIndex = configuration["AzureSearch:IndexName"];
        if (!string.IsNullOrWhiteSpace(searchEndpoint) && !string.IsNullOrWhiteSpace(searchIndex))
        {
            services.AddSingleton(new SearchClient(new Uri(searchEndpoint), searchIndex, new DefaultAzureCredential()));
            if (string.Equals(ragProvider, "AzureOpenAI", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(azureOpenAiEndpoint)
                && !string.IsNullOrWhiteSpace(embeddingDeployment))
            {
                services.AddScoped<IHybridRetriever, AzureAiSearchHybridRetriever>();
            }
        }

        var documentIntelligenceEndpoint = configuration["AzureDocumentIntelligence:Endpoint"];
        if (!string.IsNullOrWhiteSpace(documentIntelligenceEndpoint))
        {
            services.AddSingleton(new DocumentIntelligenceClient(new Uri(documentIntelligenceEndpoint), new DefaultAzureCredential()));
        }

        var blobEndpoint = configuration["AzureStorage:BlobEndpoint"];
        if (!string.IsNullOrWhiteSpace(blobEndpoint))
        {
            services.AddSingleton(new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential()));
        }

        var serviceBusNamespace = configuration["ServiceBus:FullyQualifiedNamespace"];
        if (!string.IsNullOrWhiteSpace(serviceBusNamespace))
        {
            services.AddSingleton(new ServiceBusClient(serviceBusNamespace, new DefaultAzureCredential()));
        }

        if (!string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            services.AddSingleton(NpgsqlDataSource.Create(postgresConnectionString));
            if (useLocalPostgresIndex && string.Equals(ragProvider, "Ollama", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<IDocumentIndexStore, PostgresDocumentIndexStore>();
                services.AddScoped<IHybridRetriever, PostgresVectorRetriever>();
                services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
                services.AddScoped<IRagOrchestrator, RagOrchestrator>();
                services.AddHostedService<DatabaseSchemaInitializer>();
            }
        }

        return services;
    }

    private static Uri GetServiceBaseUri(string baseUrl)
    {
        var normalized = baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : $"{baseUrl}/";
        return new Uri(normalized, UriKind.Absolute);
    }
}
