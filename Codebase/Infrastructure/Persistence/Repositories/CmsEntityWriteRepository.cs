using CmsEvents.Application.Persistence.Abstractions;
using CmsEvents.Domain.Entities;
using CmsEvents.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace CmsEvents.Infrastructure.Persistence.Repositories;

public class CmsEntityWriteRepository(WriteDbContext writeDbContext) : ICmsEntityWriteRepository
{
    public async Task<CmsEntity?> GetByIdForUpdateAsync(string id, CancellationToken cancellationToken)
    {
        return await writeDbContext.CmsEntities.FindAsync([id], cancellationToken);
    }

    public Task AddAsync(CmsEntity entity, CancellationToken cancellationToken)
    {
        return writeDbContext.CmsEntities.AddAsync(entity, cancellationToken).AsTask();
    }

    public async Task<bool> RemoveByIdAsync(string id, CancellationToken cancellationToken)
    {
        var affected = await writeDbContext.CmsEntities.Where(e => e.Id == id).ExecuteDeleteAsync(cancellationToken);
        return affected > 0;
    }
}
