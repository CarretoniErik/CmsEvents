using CmsEvents.Application.Persistence;

namespace CmsEvents.UnitTests.Infrastructure;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }
    public List<object> DetachedEntities { get; } = [];
    public void Detach(object entity) => DetachedEntities.Add(entity);
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }
}