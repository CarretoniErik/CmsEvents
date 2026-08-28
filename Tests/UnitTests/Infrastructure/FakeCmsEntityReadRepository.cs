using CmsEvents.Application.Persistence.Abstractions;
using CmsEvents.Domain.Entities;

namespace CmsEvents.UnitTests.Infrastructure;

public sealed class FakeCmsEntityReadRepository : ICmsEntityReadRepository
{
    public IReadOnlyList<CmsEntity> AllEntities { get; set; } = [];
    public IReadOnlyList<CmsEntity> VisibleEntities { get; set; } = [];

    public int ListCallCount { get; private set; }
    public int ListVisibleToUsersCallCount { get; private set; }

    public Task<IReadOnlyList<CmsEntity>> ListAsync(CancellationToken cancellationToken)
    {
        ListCallCount++;
        return Task.FromResult(AllEntities);
    }

    public Task<IReadOnlyList<CmsEntity>> ListVisibleToUsersAsync(CancellationToken cancellationToken)
    {
        ListVisibleToUsersCallCount++;
        return Task.FromResult(VisibleEntities);
    }
}