using CmsEvents.Application.Persistence.Abstractions;
using CmsEvents.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CmsEvents.Application.UseCases.ProcessCmsEvents;

public sealed class ProcessCmsEventsUseCase
(
    ILogger<ProcessCmsEventsUseCase> logger,
    ICmsEventSanitizer sanitizer,
    IValidator<ProcessCmsEventsInput> validator,
    ICmsEntityWriteRepository writeRepository,
    IConcurrencyConflictHandler concurrencyHandler
) : IProcessCmsEventsUseCase
{
    public async Task<ProcessBatchResult> ProcessBatchAsync(IReadOnlyList<ProcessCmsEventsInput> inputs, CancellationToken cancellationToken)
    {
        if (inputs is null || inputs.Count == 0)
        {
            logger.LogInformation("Received empty CMS event batch - nothing to process");
            return ProcessBatchResult.Empty;
        }

        var result = new ProcessBatchResult();
        foreach (var group in inputs.GroupBy(x => x.Id))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var winner = group
                .OrderByDescending(x => x.Timestamp)
                .ThenByDescending(x => x.Version ?? int.MaxValue)
                .First();

            // Every non-winner is by default ignored, since only the winner has to be processed
            var losers = group.Count() - 1;
            for (var i = 0; i < losers; i++) result.Register(EventOutcome.Ignored);
            if (losers > 0) logger.LogInformation("Ignored {Count} superseded event(s) for id {Id} within the batch. Processing the latest", losers, group.Key);

            try
            {
                var outcome = await ProcessSingleAsync(winner, cancellationToken);
                result.Register(outcome);
                logger.LogInformation("Processed CMS event. Type={Type} Id={Id} Version={Version} Outcome={Outcome}", winner.Type, winner.Id, winner.Version, outcome);
            }            
            catch (Exception ex)
            {
                // Resilient batch - one bad event does not abort the others
                result.Register(EventOutcome.Failed);
                logger.LogError(ex, "Failed to process CMS event. Type={Type} Id={Id} Version={Version}", winner.Type, winner.Id, winner.Version);
            }
        }

        logger.LogInformation("CMS batch processed. Total={Total} Applied={Applied} Ignored={Ignored} Deleted={Deleted} Failed={Failed}",
            inputs.Count, result.Applied, result.Ignored, result.Deleted, result.Failed);

        return result;
    }

    private async Task<EventOutcome> ProcessSingleAsync(ProcessCmsEventsInput input, CancellationToken cancellationToken)
    {
        var sanitized = sanitizer.Sanitize(input);
        var validationResult = await validator.ValidateAsync(sanitized, cancellationToken);

        if (!validationResult.IsValid)
        {
            logger.LogWarning
            (
                "Invalid CMS event. Id={Id} Type={Type} Errors={Errors}",
                sanitized.Id,
                sanitized.Type,
                validationResult.Errors.Select(x => x.ErrorMessage)
            );

            return EventOutcome.Failed;
        }

        try
        {
            return await concurrencyHandler.ResolveConflictAsync
            (
                async () => sanitized.Type switch
                {
                    CmsEventTypes.Publish => await HandlePublishAsync(sanitized, cancellationToken),
                    CmsEventTypes.Unpublish => await HandleUnpublishAsync(sanitized, cancellationToken),
                    CmsEventTypes.Delete => await HandleDeleteAsync(sanitized, cancellationToken),
                    _ => HandleUnknown(sanitized),
                },
                cancellationToken
            );
        }
        catch (ApplicationException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict for {Id} after retries. Event ignored.", input.Id);
            return EventOutcome.Ignored;
        }
    }

    private async Task<EventOutcome> HandlePublishAsync(ProcessCmsEventsInput input, CancellationToken cancellationToken)
    {
        var (version, payload) = GetVersionAndPayload(input);
        var entity = await writeRepository.GetByIdForUpdateAsync(input.Id, cancellationToken);
        if (entity is null)
        {
            // New entity (add) - First time we see it (version acts as the initial version)
            var created = CmsEntity.Create(input.Id, version, payload, input.Timestamp);
            await writeRepository.AddAsync(created, cancellationToken);
            return EventOutcome.Applied;
        }

        // Existing entity (update) - but only if the incoming version is newer
        var applied = entity.TryApplyPublish(version, payload, input.Timestamp);
        return applied ? EventOutcome.Applied : EventOutcome.Ignored;
    }

    private async Task<EventOutcome> HandleUnpublishAsync(ProcessCmsEventsInput input, CancellationToken cancellationToken)
    {
        var (version, payload) = GetVersionAndPayload(input);
        var entity = await writeRepository.GetByIdForUpdateAsync(input.Id, cancellationToken);
        if (entity is null)
        {
            // Corner case: we never had a published version, but the unpublish event carries the payload/version - materialize it, already unpublished
            var created = CmsEntity.Create(input.Id, version, payload, input.Timestamp);
            created.TryApplyUnpublish(version, payload, input.Timestamp);
            await writeRepository.AddAsync(created, cancellationToken);
            return EventOutcome.Applied;
        }

        // Cover the version gap using the event payload, then mark unpublished
        var applied = entity.TryApplyUnpublish(version, payload, input.Timestamp);
        return applied ? EventOutcome.Applied : EventOutcome.Ignored;
    }

    private async Task<EventOutcome> HandleDeleteAsync(ProcessCmsEventsInput input, CancellationToken cancellationToken)
    {
        // Hard delete
        var removed = await writeRepository.RemoveByIdAsync(input.Id, cancellationToken);
        return removed ? EventOutcome.Deleted : EventOutcome.Ignored;
    }

    private EventOutcome HandleUnknown(ProcessCmsEventsInput input)
    {
        // Covering the part "Events include, but are not limited to..." - never break the batch
        logger.LogWarning("Received unknown CMS event type. Type={Type} Id={Id}. Event ignored", input.Type, input.Id);
        return EventOutcome.Ignored;
    }

    private static (int Version, JsonDocument Payload) GetVersionAndPayload(ProcessCmsEventsInput input)
    {
        return 
        (
            input.Version!.Value,
            JsonDocument.Parse(input.Payload!.Value.GetRawText())
        );
    }
}