using CmsEvents.Infrastructure.Persistence.DbContext;
using CmsEvents.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CmsEvents.IntegrationTests.Fixtures;

public sealed class IntegrationTestFixture
{
    public TestDatabase Database { get; }
    public CmsEventsApiFactory ApiFactory { get; }
    public IntegrationTestFixture()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<IntegrationTestFixture>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var testConnectionString = configuration["TEST_DATABASE_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(testConnectionString)) throw new InvalidOperationException("TEST_DATABASE_CONNECTION_STRING is not configured");

        Database = new TestDatabase(testConnectionString);
        ApiFactory = new CmsEventsApiFactory(testConnectionString);

        // Forces host construction (applies ConfigureTestServices) and ensures the schema exists
        using var scope = ApiFactory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<WriteDbContext>().Database.EnsureCreated();
    }

    public void Dispose() => ApiFactory.Dispose();
}