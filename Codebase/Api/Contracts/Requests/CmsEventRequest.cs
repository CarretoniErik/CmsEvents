using System.Text.Json;

namespace CmsEvents.Api.Contracts.Requests;

public sealed record CmsEventRequest(string Type, string Id, JsonElement? Payload, int? Version, DateTimeOffset Timestamp);