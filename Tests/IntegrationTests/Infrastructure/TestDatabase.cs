using Npgsql;

namespace CmsEvents.IntegrationTests.Infrastructure;

public sealed class TestDatabase(string testConnectionString)
{
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(testConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            TRUNCATE TABLE cms_entities;
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}