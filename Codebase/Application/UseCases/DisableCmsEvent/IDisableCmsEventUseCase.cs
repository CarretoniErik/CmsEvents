namespace CmsEvents.Application.UseCases.DisableCmsEvent;

public interface IDisableCmsEventUseCase
{
    Task<DisableCmsEventResult> ExecuteAsync(string id, CancellationToken cancellationToken);
}
