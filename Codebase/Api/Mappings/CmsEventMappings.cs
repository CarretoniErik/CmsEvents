using CmsEvents.Api.Contracts.Requests;
using CmsEvents.Application.UseCases.ProcessCmsEvents;

namespace CmsEvents.Api.Mappings;

public static class CmsEventMappings
{
    public static ProcessCmsEventsInput ToApplicationInput(this CmsEventRequest request)
    {
        return new ProcessCmsEventsInput(request.Type, request.Id, request.Payload, request.Version, request.Timestamp);
    }

    public static IReadOnlyList<ProcessCmsEventsInput> ToApplicationInputs(this IReadOnlyList<CmsEventRequest> events)
    {
        return [.. events.Select(x => x.ToApplicationInput())];
    }
}
