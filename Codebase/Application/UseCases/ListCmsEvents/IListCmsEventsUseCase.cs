namespace CmsEvents.Application.UseCases.ListCmsEvents;

public interface IListCmsEventsUseCase
{
    Task<IReadOnlyList<CmsEventListItem>> ExecuteAsync(ListCmsEventsInput input, CancellationToken cancellationToken);
}