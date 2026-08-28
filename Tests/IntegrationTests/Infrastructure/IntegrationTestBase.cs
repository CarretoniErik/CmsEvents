using CmsEvents.IntegrationTests.Fixtures;

namespace CmsEvents.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase(IntegrationTestFixture fixture) : IAsyncLifetime
{
    protected IntegrationTestFixture Fixture { get; } = fixture;
    public Task InitializeAsync() => Fixture.Database.ClearAsync();
    public Task DisposeAsync() => Task.CompletedTask;
}