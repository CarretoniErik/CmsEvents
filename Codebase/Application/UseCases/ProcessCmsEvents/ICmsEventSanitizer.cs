namespace CmsEvents.Application.UseCases.ProcessCmsEvents;

public interface ICmsEventSanitizer
{
    ProcessCmsEventsInput Sanitize(ProcessCmsEventsInput input);
}