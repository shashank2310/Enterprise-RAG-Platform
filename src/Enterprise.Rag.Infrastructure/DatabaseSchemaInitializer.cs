using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Enterprise.Rag.Infrastructure;

public sealed class DatabaseSchemaInitializer(NpgsqlDataSource dataSource, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!bool.TryParse(configuration["Rag:EnableLocalPostgresIndex"], out var localIndexEnabled) || !localIndexEnabled)
        {
            return;
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            "ALTER TABLE document_chunks ADD COLUMN IF NOT EXISTS embedding jsonb",
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
