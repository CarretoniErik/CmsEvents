using CmsEvents.Application.Persistence.Abstractions;
using CmsEvents.Domain.Entities;

namespace CmsEvents.UnitTests.Infrastructure;

public sealed class FakeCmsEntityWriteRepository : ICmsEntityWriteRepository
{
    public Dictionary<string, CmsEntity> Entities { get; } = [];
    public List<CmsEntity> Added { get; } = [];
    public List<string> RemovedIds { get; } = [];
    public string? FailureId { get; init; }

    public Task<CmsEntity?> GetByIdForUpdateAsync(string id, CancellationToken cancellationToken)
    {
        if (id == FailureId) throw new InvalidOperationException("Simulated repository failure");
        Entities.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task AddAsync(CmsEntity entity, CancellationToken cancellationToken)
    {
        Added.Add(entity);
        Entities[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task<bool> RemoveByIdAsync(string id, CancellationToken cancellationToken)
    {
        if (id == FailureId) throw new InvalidOperationException("Simulated repository failure");
        RemovedIds.Add(id);
        return Task.FromResult(Entities.Remove(id));
    }
}
