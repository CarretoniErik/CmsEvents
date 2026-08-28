using System.Text.Json;

namespace CmsEvents.Domain.Entities;

public sealed class CmsEntity
{
    private CmsEntity() { }

    private CmsEntity(string id, int version, JsonDocument payload, DateTimeOffset cmsTimestamp)
    {
        Id = id;
        Version = version;
        Payload = payload;
        CmsTimestamp = cmsTimestamp;
    }

    /// <summary>Entity identifier assigned by the CMS (source of truth)</summary>
    public string Id { get; private set; } = default!;

    /// <summary>Latest version known to this service. Assigned by the CMS, never incremented locally</summary>
    public int Version { get; private set; }

    /// <summary>Open/generic entity data as received from the CMS</summary>
    public JsonDocument Payload { get; private set; } = default!;

    /// <summary>Timestamp of the CMS event that last mutated this entity</summary>
    public DateTimeOffset CmsTimestamp { get; private set; }

    /// <summary>True when the CMS unpublished this entity (kept in storage, hidden from normal users)</summary>
    public bool IsUnpublishedByCms { get; private set; }

    /// <summary>Admin override, independent from the CMS state. Does not affect CMS data</summary>
    public bool IsDisabledByAdmin { get; private set; }

    /// <summary>Visible to a normal (non-admin) consumer only when neither disable axis is active</summary>
    public bool IsVisibleToUsers => !IsUnpublishedByCms && !IsDisabledByAdmin;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static CmsEntity Create(string id, int version, JsonDocument payload, DateTimeOffset cmsTimestamp)
    {
        var now = DateTimeOffset.UtcNow;
        return new CmsEntity(id, version, payload, cmsTimestamp)
        {
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// Applies an incoming published version. Returns false (ignored) if the incoming
    /// version is not newer than the persisted one (idempotency / out-of-order tolerance)
    /// </summary>
    public bool TryApplyPublish(int incomingVersion, JsonDocument payload, DateTimeOffset cmsTimestamp)
    {
        if (incomingVersion <= Version) return false;

        Version = incomingVersion;
        Payload = payload;
        CmsTimestamp = cmsTimestamp;
        IsUnpublishedByCms = false; // a fresh publish re-publishes the entity
        Touch();
        return true;
    }

    /// <summary>
    /// Applies an unpublish. Uses the event payload to cover the version gap
    /// (the entity may have been modified without a prior publish), then marks it unpublished
    /// </summary>
    public bool TryApplyUnpublish(int incomingVersion, JsonDocument payload, DateTimeOffset cmsTimestamp)
    {
        if (incomingVersion < Version) return false;

        Version = incomingVersion;
        Payload = payload;
        CmsTimestamp = cmsTimestamp;
        IsUnpublishedByCms = true;
        Touch();
        return true;
    }

    public void DisableByAdmin()
    {
        if (IsDisabledByAdmin) return;
        IsDisabledByAdmin = true;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}