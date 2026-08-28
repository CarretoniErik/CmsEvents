using System.Text.Json;

namespace CmsEvents.Application.UseCases.ProcessCmsEvents;

public sealed record ProcessCmsEventsInput(string Type, string Id, JsonElement? Payload, int? Version, DateTimeOffset Timestamp);