using CmsEvents.Application.Persistence.Abstractions;
using CmsEvents.Domain.Entities;
using CmsEvents.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace CmsEvents.Infrastructure.Persistence.Repositories;

public class CmsEntityReadRepository(ReadDbContext readDbContext) : ICmsEntityReadRepository
{
    public async Task<IReadOnlyList<CmsEntity>> ListAsync(CancellationToken cancellationToken)
    {
        return await readDbContext.CmsEntities.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CmsEntity>> ListVisibleToUsersAsync(CancellationToken cancellationToken)
    {
        return await readDbContext.CmsEntities.Where(x => !x.IsUnpublishedByCms && !x.IsDisabledByAdmin).ToListAsync(cancellationToken);
    }
}
