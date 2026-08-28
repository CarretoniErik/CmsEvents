namespace CmsEvents.Application.UseCases.ProcessCmsEvents;

public interface IProcessCmsEventsUseCase
{
    Task<ProcessBatchResult> ProcessBatchAsync(IReadOnlyList<ProcessCmsEventsInput> events, CancellationToken cancellationToken);
}