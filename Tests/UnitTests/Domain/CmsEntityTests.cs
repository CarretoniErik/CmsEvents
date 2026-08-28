using CmsEvents.Domain.Entities;
using FluentAssertions;
using System.Text.Json;

namespace CmsEvents.UnitTests.Domain;

public sealed class CmsEntityTests
{
    [Fact]
    public void ShouldApplyPublishWhenIncomingVersionIsNewer()
    {
        // Arrange
        var entity = CreateEntity(version: 1);
        using var payload = JsonDocument.Parse("""{"name":"v2"}""");
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var applied = entity.TryApplyPublish(2, payload, timestamp);

        // Assert
        applied.Should().BeTrue();
        entity.Version.Should().Be(2);
        entity.Payload.RootElement.GetProperty("name").GetString().Should().Be("v2");
        entity.CmsTimestamp.Should().Be(timestamp);
        entity.IsUnpublishedByCms.Should().BeFalse();
    }

    [Fact]
    public void ShouldIgnorePublishWhenIncomingVersionIsNotNewer()
    {
        // Arrange
        var entity = CreateEntity(version: 2);
        using var payload = JsonDocument.Parse("""{"name":"old"}""");

        // Act
        var applied = entity.TryApplyPublish(1, payload, DateTimeOffset.UtcNow);

        // Assert
        applied.Should().BeFalse();
        entity.Version.Should().Be(2);
        entity.Payload.RootElement.GetProperty("name").GetString().Should().Be("initial");
    }

    [Fact]
    public void ShouldIgnorePublishWhenIncomingVersionIsEqual()
    {
        // Arrange
        var entity = CreateEntity(version: 2);
        using var payload = JsonDocument.Parse("""{"name":"duplicate"}""");

        // Act
        var applied = entity.TryApplyPublish(2, payload, DateTimeOffset.UtcNow);

        // Assert
        applied.Should().BeFalse();
        entity.Version.Should().Be(2);
        entity.Payload.RootElement.GetProperty("name").GetString().Should().Be("initial");
    }

    [Fact]
    public void ShouldUnpublishEntity()
    {
        // Arrange
        var entity = CreateEntity(version: 2);
        using var payload = JsonDocument.Parse("""{"name":"v2"}""");
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var applied = entity.TryApplyUnpublish(2, payload, timestamp);

        // Assert
        applied.Should().BeTrue();
        entity.Version.Should().Be(2);
        entity.IsUnpublishedByCms.Should().BeTrue();
        entity.IsVisibleToUsers.Should().BeFalse();
    }

    [Fact]
    public void ShouldMaterializeLatestVersionWhenUnpublishingPreviouslyUnpublishedVersion()
    {
        // Arrange
        var entity = CreateEntity(version: 1);
        using var payload = JsonDocument.Parse("""{"name":"version-2"}""");

        // Act
        var applied = entity.TryApplyUnpublish(2, payload, DateTimeOffset.UtcNow);

        // Assert
        applied.Should().BeTrue();
        entity.Version.Should().Be(2);
        entity.Payload.RootElement.GetProperty("name").GetString().Should().Be("version-2");
        entity.IsUnpublishedByCms.Should().BeTrue();
        entity.IsVisibleToUsers.Should().BeFalse();
    }

    [Fact]
    public void ShouldRepublishPreviouslyUnpublishedEntity()
    {
        // Arrange
        var entity = CreateEntity(version: 1);
        using var unpublishPayload = JsonDocument.Parse("""{"name":"v1"}""");
        entity.TryApplyUnpublish(1, unpublishPayload, DateTimeOffset.UtcNow);
        using var publishPayload = JsonDocument.Parse("""{"name":"v2"}""");

        // Act
        var applied = entity.TryApplyPublish(2, publishPayload,DateTimeOffset.UtcNow);

        // Assert
        applied.Should().BeTrue();
        entity.Version.Should().Be(2);
        entity.IsUnpublishedByCms.Should().BeFalse();
        entity.IsVisibleToUsers.Should().BeTrue();
    }

    [Fact]
    public void ShouldDisableEntityByAdmin()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        entity.DisableByAdmin();

        // Assert
        entity.IsDisabledByAdmin.Should().BeTrue();
        entity.IsVisibleToUsers.Should().BeFalse();
    }

    [Fact]
    public void ShouldKeepAdminDisableWhenCmsPublishesNewVersion()
    {
        // Arrange
        var entity = CreateEntity(version: 1);
        entity.DisableByAdmin();
        using var payload = JsonDocument.Parse("""{"name":"v2"}""");

        // Act
        entity.TryApplyPublish(2, payload, DateTimeOffset.UtcNow);

        // Assert
        entity.IsDisabledByAdmin.Should().BeTrue();
        entity.IsUnpublishedByCms.Should().BeFalse();
        entity.IsVisibleToUsers.Should().BeFalse();
    }

    private static CmsEntity CreateEntity(int version = 1)
    {
        return CmsEntity.Create("entity-1", version, JsonDocument.Parse("""{"name":"initial"}"""), DateTimeOffset.UtcNow);
    }
}
