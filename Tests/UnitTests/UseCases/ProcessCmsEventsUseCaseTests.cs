using CmsEvents.Application.UseCases.ProcessCmsEvents;
using CmsEvents.Domain.Entities;
using CmsEvents.UnitTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace CmsEvents.UnitTests.UseCases;

public sealed class ProcessCmsEventsUseCaseTests
{
    [Fact]
    public async Task ShouldCreateEntityWhenPublishEventIsReceived()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);
        var timestamp = DateTimeOffset.UtcNow;
        var payload = CreatePayload();
        var events = new[]
        {
            new ProcessCmsEventsInput("publish", "event-1", payload, 1, timestamp)
        };

        // Act
        var result = await sut.ProcessBatchAsync(events, CancellationToken.None);

        // Assert
        result.Applied.Should().Be(1);
        result.Ignored.Should().Be(0);
        result.Failed.Should().Be(0);
        repository.Added.Should().ContainSingle();
        var entity = repository.Added.Single();
        entity.Id.Should().Be("event-1");
        entity.Version.Should().Be(1);
        entity.IsUnpublishedByCms.Should().BeFalse();
        entity.IsVisibleToUsers.Should().BeTrue();
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldApplyNewerPublishVersion()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);
        var entity = CreateEntity(id: "event-1", version: 1, timestamp: DateTimeOffset.UtcNow.AddMinutes(-1));
        repository.Entities["event-1"] = entity;
        var events = new[]
        {
            new ProcessCmsEventsInput("publish", "event-1", CreatePayload("new"), 2, DateTimeOffset.UtcNow)
        };

        // Act
        var result = await sut.ProcessBatchAsync(events, CancellationToken.None);

        // Assert
        result.Applied.Should().Be(1);
        result.Ignored.Should().Be(0);
        result.Failed.Should().Be(0);
        entity.Version.Should().Be(2);
        entity.IsUnpublishedByCms.Should().BeFalse();
        entity.IsVisibleToUsers.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldIgnoreOlderPublishVersion()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);
        var entity = CreateEntity(id: "event-1", version: 5, timestamp: DateTimeOffset.UtcNow.AddMinutes(-1));
        repository.Entities["event-1"] = entity;

        // Act
        var result = await sut.ProcessBatchAsync
        (
            [
                new ProcessCmsEventsInput("publish", "event-1", CreatePayload("old"), 4, DateTimeOffset.UtcNow)
            ],
            CancellationToken.None
        );

        // Assert
        result.Applied.Should().Be(0);
        result.Ignored.Should().Be(1);
        result.Failed.Should().Be(0);
        entity.Version.Should().Be(5);
    }

    [Fact]
    public async Task ShouldMarkEntityAsUnpublished()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);
        var entity = CreateEntity(id: "event-1", version: 1, timestamp: DateTimeOffset.UtcNow.AddMinutes(-1));
        repository.Entities["event-1"] = entity;

        // Act
        var result = await sut.ProcessBatchAsync
        (
            [
                new ProcessCmsEventsInput("unpublish", "event-1", CreatePayload("unpublished"), 2, DateTimeOffset.UtcNow)
            ],
            CancellationToken.None
        );

        // Assert
        result.Applied.Should().Be(1);
        result.Ignored.Should().Be(0);
        result.Failed.Should().Be(0);
        entity.Version.Should().Be(2);
        entity.IsUnpublishedByCms.Should().BeTrue();
        entity.IsVisibleToUsers.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldDeleteEntityWhenDeleteEventIsReceived()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);
        repository.Entities["event-1"] = CreateEntity(id: "event-1", version: 1, timestamp: DateTimeOffset.UtcNow);

        // Act
        var result = await sut.ProcessBatchAsync
        (
            [
                new ProcessCmsEventsInput("delete", "event-1", null, null, DateTimeOffset.UtcNow)
            ],
            CancellationToken.None
        );

        // Assert
        result.Deleted.Should().Be(1);
        result.Applied.Should().Be(0);
        result.Ignored.Should().Be(0);
        result.Failed.Should().Be(0);
        repository.Entities.Should().NotContainKey("event-1");
        repository.RemovedIds.Should().ContainSingle().Which.Should().Be("event-1");
    }

    [Fact]
    public async Task ShouldIgnoreUnknownEventType()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);

        // Act
        var result = await sut.ProcessBatchAsync
        (
            [
                new ProcessCmsEventsInput("something-new", "event-1", CreatePayload(), 1, DateTimeOffset.UtcNow)
            ],
            CancellationToken.None
        );

        // Assert
        result.Applied.Should().Be(0);
        result.Ignored.Should().Be(1);
        result.Deleted.Should().Be(0);
        result.Failed.Should().Be(0);
        repository.Added.Should().BeEmpty();
        repository.RemovedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldProcessOnlyLatestEventForSameId()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);
        var timestamp = DateTimeOffset.UtcNow;
        var events = new[]
        {
            new ProcessCmsEventsInput("publish", "event-1", CreatePayload("old"), 1, timestamp.AddMinutes(-2)),
            new ProcessCmsEventsInput("publish", "event-1", CreatePayload("latest"), 2, timestamp)
        };

        // Act
        var result = await sut.ProcessBatchAsync(events, CancellationToken.None);

        // Assert
        result.Applied.Should().Be(1);
        result.Ignored.Should().Be(1);
        result.Failed.Should().Be(0);
        repository.Added.Should().ContainSingle();
        var entity = repository.Added.Single();
        entity.Id.Should().Be("event-1");
        entity.Version.Should().Be(2);
        entity.Payload.RootElement.GetProperty("value").GetString().Should().Be("latest");
    }

    [Fact]
    public async Task ShouldContinueProcessingBatchWhenOneEventFails()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository { FailureId = "event-fails" };
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);
        var events = new[]
        {
            new ProcessCmsEventsInput("publish", "event-fails", CreatePayload("bad"), 1, DateTimeOffset.UtcNow),
            new ProcessCmsEventsInput("publish", "event-succeeds", CreatePayload("good"), 1, DateTimeOffset.UtcNow)
        };

        // Act
        var result = await sut.ProcessBatchAsync(events, CancellationToken.None);

        // Assert
        result.Applied.Should().Be(1);
        result.Failed.Should().Be(1);
        result.Ignored.Should().Be(0);
        repository.Added.Should().ContainSingle();
        repository.Added.Single().Id.Should().Be("event-succeeds");
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldIgnorePublishWithSameVersion()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);
        var entity = CreateEntity(id: "event-1", version: 5, timestamp: DateTimeOffset.UtcNow.AddMinutes(-1));
        repository.Entities["event-1"] = entity;

        // Act
        var result = await sut.ProcessBatchAsync
        (
            [
                new ProcessCmsEventsInput
                (
                    "publish",
                    "event-1",
                    CreatePayload("duplicate"),
                    5,
                    DateTimeOffset.UtcNow
                )
            ],
            CancellationToken.None
        );

        // Assert
        result.Applied.Should().Be(0);
        result.Ignored.Should().Be(1);
        result.Failed.Should().Be(0);
        entity.Version.Should().Be(5);
    }

    [Fact]
    public async Task ShouldApplyUnpublishWithSameVersion()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);
        var entity = CreateEntity(id: "event-1", version: 5, timestamp: DateTimeOffset.UtcNow.AddMinutes(-1));
        repository.Entities["event-1"] = entity;

        // Act
        var result = await sut.ProcessBatchAsync
        (
            [
                new ProcessCmsEventsInput
                (
                    "unpublish",
                    "event-1",
                    CreatePayload("unpublished"),
                    5,
                    DateTimeOffset.UtcNow
                )
            ],
            CancellationToken.None
        );

        // Assert
        result.Applied.Should().Be(1);
        result.Ignored.Should().Be(0);
        result.Failed.Should().Be(0);
        entity.Version.Should().Be(5);
        entity.IsUnpublishedByCms.Should().BeTrue();
        entity.IsVisibleToUsers.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldIgnoreOlderUnpublishVersion()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);
        var entity = CreateEntity(id: "event-1", version: 5, timestamp: DateTimeOffset.UtcNow);
        repository.Entities["event-1"] = entity;

        // Act
        var result = await sut.ProcessBatchAsync
        (
            [
                new ProcessCmsEventsInput
                (
                    "unpublish",
                    "event-1",
                    CreatePayload("old"),
                    4,
                    DateTimeOffset.UtcNow.AddMinutes(-1)
                )
            ],
            CancellationToken.None
        );

        // Assert
        result.Applied.Should().Be(0);
        result.Ignored.Should().Be(1);
        result.Failed.Should().Be(0);
        entity.Version.Should().Be(5);
        entity.IsUnpublishedByCms.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldMaterializeEntityWhenUnpublishIsReceivedWithoutPublishedVersion()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);

        // Act
        var result = await sut.ProcessBatchAsync
        (
            [
                new ProcessCmsEventsInput
                (
                    "unpublish",
                    "event-1",
                    CreatePayload("version-2"),
                    2,
                    DateTimeOffset.UtcNow
                )
            ],
            CancellationToken.None
        );

        // Assert
        result.Applied.Should().Be(1);
        result.Ignored.Should().Be(0);
        result.Failed.Should().Be(0);
        repository.Added.Should().ContainSingle();
        var entity = repository.Added.Single();
        entity.Id.Should().Be("event-1");
        entity.Version.Should().Be(2);
        entity.IsUnpublishedByCms.Should().BeTrue();
        entity.IsVisibleToUsers.Should().BeFalse();
        entity.Payload.RootElement
            .GetProperty("value")
            .GetString()
            .Should()
            .Be("version-2");
    }

    [Fact]
    public async Task ShouldRepublishPreviouslyUnpublishedEntity()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);

        var entity = CreateEntity(id: "event-1", version: 2, timestamp: DateTimeOffset.UtcNow.AddMinutes(-1));
        entity.TryApplyUnpublish(2, CreatePayloadDocument("unpublished"), DateTimeOffset.UtcNow.AddMinutes(-1));
        repository.Entities["event-1"] = entity;

        // Act
        var result = await sut.ProcessBatchAsync
        (
            [
                new ProcessCmsEventsInput
                (
                    "publish",
                    "event-1",
                    CreatePayload("republished"),
                    3,
                    DateTimeOffset.UtcNow
                )
            ],
            CancellationToken.None
        );

        // Assert
        result.Applied.Should().Be(1);
        result.Ignored.Should().Be(0);
        result.Failed.Should().Be(0);
        entity.Version.Should().Be(3);
        entity.IsUnpublishedByCms.Should().BeFalse();
        entity.IsVisibleToUsers.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldProcessHigherVersionWhenEventsHaveSameTimestamp()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(repository, unitOfWork);
        var timestamp = DateTimeOffset.UtcNow;
        var events = new[]
        {
            new ProcessCmsEventsInput
            (
                "publish",
                "event-1",
                CreatePayload("version-1"),
                1,
                timestamp
            ),
            new ProcessCmsEventsInput
            (
                "publish",
                "event-1",
                CreatePayload("version-2"),
                2,
                timestamp
            )
        };

        // Act
        var result = await sut.ProcessBatchAsync(events, CancellationToken.None);

        // Assert
        result.Applied.Should().Be(1);
        result.Ignored.Should().Be(1);
        result.Failed.Should().Be(0);
        repository.Added.Should().ContainSingle();
        var entity = repository.Added.Single();
        entity.Version.Should().Be(2);
        entity.Payload.RootElement.GetProperty("value")
            .GetString()
            .Should()
            .Be("version-2");
    }

    private static ProcessCmsEventsUseCase CreateSut(FakeCmsEntityWriteRepository repository, FakeUnitOfWork unitOfWork)
    {
        return new ProcessCmsEventsUseCase
        (
            NullLogger<ProcessCmsEventsUseCase>.Instance,
            new FakeCmsEventSanitizer(),
            new AcceptAllValidator(),
            repository,
            unitOfWork,
            new FakeConcurrencyConflictHandler()
        );
    }

    private static CmsEntity CreateEntity(string id, int version, DateTimeOffset timestamp)
    {
        return CmsEntity.Create(id, version, CreatePayloadDocument(), timestamp);
    }

    private static JsonDocument CreatePayloadDocument(string value = "test")
    {
        return JsonDocument.Parse($$"""{"value":"{{value}}"}""");
    }

    private static JsonElement CreatePayload(string value = "test")
    {
        using var document = JsonDocument.Parse($$"""{"value":"{{value}}"}""");
        return document.RootElement.Clone();
    }
}