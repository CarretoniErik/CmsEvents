using CmsEvents.Api.Contracts.Requests;
using CmsEvents.Api.Mappings;
using CmsEvents.Application.UseCases.ProcessCmsEvents;

namespace CmsEvents.Api.Endpoints;

public static class CmsEndpoints
{
    public static IEndpointRouteBuilder MapCmsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/cms")
            .RequireAuthorization("Cms")
            .WithTags("CMS");

        group
            .WithName("IngestCmsEvents")
            .WithSummary("Ingests a batch of CMS events")
            .MapPost("/events", async (IReadOnlyList<CmsEventRequest> events, IProcessCmsEventsUseCase useCase, CancellationToken cancellationToken) =>
            {
                var inputs = events.ToApplicationInputs();
                var result = await useCase.ProcessBatchAsync(inputs, cancellationToken);
                return Results.Ok(result);
            });

        return app;
    }
}