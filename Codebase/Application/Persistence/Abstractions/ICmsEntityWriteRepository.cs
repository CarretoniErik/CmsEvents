using CmsEvents.Domain.Entities;

namespace CmsEvents.Application.Persistence.Abstractions;

public interface ICmsEntityWriteRepository
{
    Task<CmsEntity?> GetByIdForUpdateAsync(string id, CancellationToken cancellationToken);
    Task AddAsync(CmsEntity entity, CancellationToken cancellationToken);
    Task<bool> RemoveByIdAsync(string id, CancellationToken cancellationToken);
}
