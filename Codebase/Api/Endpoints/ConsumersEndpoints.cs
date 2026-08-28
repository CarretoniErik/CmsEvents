using CmsEvents.Application.UseCases.DisableCmsEvent;
using CmsEvents.Application.UseCases.ListCmsEvents;
using System.Security.Claims;

namespace CmsEvents.Api.Endpoints;

public static class ConsumersEndpoints
{
    public static IEndpointRouteBuilder MapConsumersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/consumers")
            .RequireAuthorization("Consumer")
            .WithTags("Consumers");

        group
            .MapGet("/cms/events", async (ClaimsPrincipal user, IListCmsEventsUseCase useCase, CancellationToken cancellationToken) =>
            {
                var input = new ListCmsEventsInput(user.IsInRole("Admin"));
                var result = await useCase.ExecuteAsync(input, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("ListCmsEvents")
            .WithSummary("Lists CMS events");

        group
            .MapDelete("/cms/events/{id}", async (string id, IDisableCmsEventUseCase useCase, CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);

                return result switch
                {
                    DisableCmsEventResult.Success => Results.NoContent(),
                    DisableCmsEventResult.NotFound => Results.NotFound(),
                    _ => Results.BadRequest()
                };
            })
            .WithName("DisableCmsEvent")
            .WithSummary("Disables a CMS event")
            .RequireAuthorization("Admin");

        return app;
    }
}