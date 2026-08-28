using CmsEvents.Application.UseCases.ListCmsEvents;
using CmsEvents.Domain.Entities;
using CmsEvents.UnitTests.Infrastructure;
using FluentAssertions;
using System.Text.Json;

namespace CmsEvents.UnitTests.UseCases;

public sealed class ListCmsEventsUseCaseTests
{
    [Fact]
    public async Task ShouldUseAllEventsForAdmin()
    {
        // Arrange
        var repository = new FakeCmsEntityReadRepository
        {
            AllEntities =
            [
                CreateEntity("1", 1, isUnpublished: false, isDisabled: false),
                CreateEntity("2", 2, isUnpublished: true, isDisabled: false),
                CreateEntity("3", 3, isUnpublished: false, isDisabled: true)
            ]
        };
        repository.VisibleEntities = [repository.AllEntities[0]];
        var useCase = new ListCmsEventsUseCase(repository);

        // Act
        var result = await useCase.ExecuteAsync(new ListCmsEventsInput(IsAdmin: true), CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        repository.ListCallCount.Should().Be(1);
        repository.ListVisibleToUsersCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ShouldUseOnlyVisibleEventsForRegularUser()
    {
        // Arrange
        var repository = new FakeCmsEntityReadRepository
        {
            AllEntities =
            [
                CreateEntity("1", 1, isUnpublished: false, isDisabled: false),
                CreateEntity("2", 2, isUnpublished: true, isDisabled: false),
                CreateEntity("3", 3, isUnpublished: false, isDisabled: true)
            ]
        };
        repository.VisibleEntities = [repository.AllEntities[0]];
        var useCase = new ListCmsEventsUseCase(repository);

        // Act
        var result = await useCase.ExecuteAsync(new ListCmsEventsInput(IsAdmin: false), CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be("1");
        repository.ListCallCount.Should().Be(0);
        repository.ListVisibleToUsersCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldMapEntitiesToCmsEventListItems()
    {
        // Arrange
        var repository = new FakeCmsEntityReadRepository();
        var timestamp = new DateTimeOffset(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);
        var entity = CreateEntity("cms-123", 7, timestamp, isUnpublished: true, isDisabled: false);
        repository.AllEntities = [entity];
        var useCase = new ListCmsEventsUseCase(repository);

        // Act
        var result = await useCase.ExecuteAsync(new ListCmsEventsInput(IsAdmin: true), CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        var item = result[0];
        item.Id.Should().Be(entity.Id);
        item.Version.Should().Be(entity.Version);
        item.Payload.Should().BeSameAs(entity.Payload);
        item.CmsTimestamp.Should().Be(entity.CmsTimestamp);
        item.IsUnpublishedByCms.Should().Be(entity.IsUnpublishedByCms);
        item.IsDisabledByAdmin.Should().Be(entity.IsDisabledByAdmin);
        item.IsVisibleToUsers.Should().Be(entity.IsVisibleToUsers);
        item.CreatedAt.Should().Be(entity.CreatedAt);
        item.UpdatedAt.Should().Be(entity.UpdatedAt);
    }

    private static CmsEntity CreateEntity(string id, int version, DateTimeOffset? timestamp = null, bool isUnpublished = false, bool isDisabled = false)
    {
        var entity = CmsEntity.Create(id, version, CreatePayload(), timestamp ?? DateTimeOffset.UtcNow);
        if (isUnpublished) entity.TryApplyUnpublish(version, CreatePayload(), timestamp ?? DateTimeOffset.UtcNow);
        if (isDisabled) entity.DisableByAdmin();
        return entity;
    }

    private static JsonDocument CreatePayload() => JsonDocument.Parse("""{"value":"test"}""");
}