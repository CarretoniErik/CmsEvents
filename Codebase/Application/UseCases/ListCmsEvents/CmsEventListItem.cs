using System.Text.Json;

namespace CmsEvents.Application.UseCases.ListCmsEvents;

public sealed record CmsEventListItem
(
    string Id,
    int Version,
    JsonDocument Payload,
    DateTimeOffset CmsTimestamp,
    bool IsUnpublishedByCms,
    bool IsDisabledByAdmin,
    bool IsVisibleToUsers,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);