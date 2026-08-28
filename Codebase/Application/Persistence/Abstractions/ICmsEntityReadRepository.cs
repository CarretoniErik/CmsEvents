using CmsEvents.Domain.Entities;

namespace CmsEvents.Application.Persistence.Abstractions;

public interface ICmsEntityReadRepository
{
    Task<IReadOnlyList<CmsEntity>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<CmsEntity>> ListVisibleToUsersAsync(CancellationToken cancellationToken);
}