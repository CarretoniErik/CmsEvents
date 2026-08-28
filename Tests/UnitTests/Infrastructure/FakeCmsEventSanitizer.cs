using CmsEvents.Application.UseCases.ProcessCmsEvents;

namespace CmsEvents.UnitTests.Infrastructure;

public sealed class FakeCmsEventSanitizer : ICmsEventSanitizer
{
    public ProcessCmsEventsInput Sanitize(ProcessCmsEventsInput input) => input;
}