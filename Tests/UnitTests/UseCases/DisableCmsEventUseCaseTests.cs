using CmsEvents.Application.UseCases.DisableCmsEvent;
using CmsEvents.Domain.Entities;
using CmsEvents.Domain.Exceptions;
using CmsEvents.UnitTests.Infrastructure;
using FluentAssertions;
using System.Text.Json;

namespace CmsEvents.UnitTests.UseCases;

public sealed class DisableCmsEventUseCaseTests
{
    [Fact]
    public async Task ShouldDisableExistingEntity()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var entity = CmsEntity.Create("cms-123", 1, CreatePayload(), DateTimeOffset.UtcNow);
        repository.Entities[entity.Id] = entity;
        var useCase = new DisableCmsEventUseCase(repository, unitOfWork);

        // Act
        await useCase.ExecuteAsync(entity.Id, CancellationToken.None);

        // Assert
        entity.IsDisabledByAdmin.Should().BeTrue();
        entity.IsVisibleToUsers.Should().BeFalse();
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldThrowDomainExceptionWhenEntityDoesNotExist()
    {
        // Arrange
        var repository = new FakeCmsEntityWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var useCase = new DisableCmsEventUseCase(repository, unitOfWork);

        // Act
        var response = await useCase.ExecuteAsync("does-not-exist", CancellationToken.None);

        // Assert
        response.Should().Be(DisableCmsEventResult.NotFound);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    private static JsonDocument CreatePayload() => JsonDocument.Parse("""{"value":"test"}""");
}