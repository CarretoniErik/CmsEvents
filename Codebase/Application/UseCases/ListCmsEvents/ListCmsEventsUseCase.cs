using CmsEvents.Application.Persistence.Abstractions;
using CmsEvents.Domain.Entities;

namespace CmsEvents.Application.UseCases.ListCmsEvents;

public class ListCmsEventsUseCase(ICmsEntityReadRepository readRepository) : IListCmsEventsUseCase
{
    public async Task<IReadOnlyList<CmsEventListItem>> ExecuteAsync(ListCmsEventsInput input, CancellationToken cancellationToken)
    {        
        IReadOnlyList<CmsEntity> entities;

        if (input.IsAdmin) entities = await readRepository.ListAsync(cancellationToken);
        else entities = await readRepository.ListVisibleToUsersAsync(cancellationToken);

        return 
        [.. entities
            .Select(entity => new CmsEventListItem
            (
                entity.Id,
                entity.Version,
                entity.Payload,
                entity.CmsTimestamp,
                entity.IsUnpublishedByCms,
                entity.IsDisabledByAdmin,
                entity.IsVisibleToUsers,
                entity.CreatedAt,
                entity.UpdatedAt
            ))
        ];
    }
}