namespace CmsEvents.Application.UseCases.ProcessCmsEvents;

public sealed class CmsEventSanitizer : ICmsEventSanitizer
{
    public ProcessCmsEventsInput Sanitize(ProcessCmsEventsInput input)
    {
        return input with
        {
            Type = input.Type.Trim().ToLowerInvariant(),
            Id = input.Id.Trim()
        };
    }
}