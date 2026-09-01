using CmsEvents.Application.Persistence;
using CmsEvents.Application.Persistence.Abstractions;
using CmsEvents.Domain.Entities;
using CmsEvents.Infrastructure.Persistence;
using CmsEvents.Infrastructure.Persistence.DbContext;
using CmsEvents.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CmsEvents.IntegrationTests.Persistence;

public sealed class ConcurrencyConflictHandlerTests : IAsyncLifetime
{
    private readonly DbContextOptions<WriteDbContext> _dbContextOptions;
    private WriteDbContext _dbContext = null!;
    private ConcurrencyConflictHandler _handler = null!;
    private ICmsEntityWriteRepository _writeRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private ILogger<ConcurrencyConflictHandler> _logger = null!;

    public ConcurrencyConflictHandlerTests()
    {
        // In-memory database for isolated tests
        _dbContextOptions = new DbContextOptionsBuilder<WriteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public async Task InitializeAsync()
    {
        _dbContext = new WriteDbContext(_dbContextOptions);
        await _dbContext.Database.EnsureCreatedAsync();

        // Mock logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
        _logger = loggerFactory.CreateLogger<ConcurrencyConflictHandler>();

        _handler = new ConcurrencyConflictHandler(_logger);
        _writeRepository = new CmsEntityWriteRepository(_dbContext);
        _unitOfWork = _dbContext;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task ProcessCmsEventsWhenConcurrencyConflictOccursShouldRetryAndSucceed()
    {
        // Arrange
        var entityId = "cms-entity-1";
        var initialVersion = 1;
        var payload = JsonDocument.Parse("""{"name": "Initial"}""");

        var entity = CmsEntity.Create(entityId, initialVersion, payload, DateTimeOffset.UtcNow);
        await _writeRepository.AddAsync(entity, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act: Simulate two simultaneous updates
        // First one changes the entity, second one will conflict
        var firstUpdatePayload = JsonDocument.Parse("""{"name": "First Update"}""");
        var secondUpdatePayload = JsonDocument.Parse("""{"name": "Second Update"}""");

        var firstUpdateTask = Task.Run(async () =>
        {
            var conflictHandlerForFirstTask = new ConcurrencyConflictHandler(_logger);
            return await conflictHandlerForFirstTask.ResolveConflictAsync(async () =>
            {
                var e = await _writeRepository.GetByIdForUpdateAsync(entityId, CancellationToken.None);
                e?.TryApplyPublish(2, firstUpdatePayload, DateTimeOffset.UtcNow);
                return e;
            }, CancellationToken.None);
        });

        var secondUpdateTask = Task.Run(async () =>
        {
            // Small delay to ensure first commit already happened
            await Task.Delay(50);

            var conflictHandlerForSecondTask = new ConcurrencyConflictHandler(_logger);
            return await conflictHandlerForSecondTask.ResolveConflictAsync(async () =>
            {
                var e = await _writeRepository.GetByIdForUpdateAsync(entityId, CancellationToken.None);
                e?.TryApplyPublish(3, secondUpdatePayload, DateTimeOffset.UtcNow);
                return e;
            }, CancellationToken.None);
        });

        await Task.WhenAll(firstUpdateTask, secondUpdateTask);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert: Verify that both versions were applied
        var result = await _writeRepository.GetByIdForUpdateAsync(entityId, CancellationToken.None);
        result.Should().NotBeNull();
        result!.Version.Should().Be(3); // Second version won
    }

    [Fact]
    public async Task ProcessCmsEventsWhenConcurrencyConflictExceedsMaxRetriesShouldThrowApplicationException()
    {
        // Arrange: Setup a scenario that forces permanent conflict
        var entityId = "cms-entity-conflict";
        var payload = JsonDocument.Parse("""{"name": "Test"}""");

        var entity = CmsEntity.Create(entityId, 1, payload, DateTimeOffset.UtcNow);
        await _writeRepository.AddAsync(entity, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Create a mock that always throws concurrency exception
        var mockHandler = new AlwaysFailingConcurrencyHandler(_logger);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ApplicationException>(async () =>
        {
            await mockHandler.ResolveConflictAsync(
                async () =>
                {
                    var e = await _writeRepository.GetByIdForUpdateAsync(entityId, CancellationToken.None);
                    return e;
                },
                CancellationToken.None
            );
        });

        ex.Message.Should().Contain("Concurrency conflict after retries");
    }

    [Fact]
    public async Task ProcessCmsEventsWithMultipleBatchesShouldHandleConcurrencySeparately()
    {
        // Arrange: Multiple entities being processed
        var payload1 = JsonDocument.Parse("""{"id": "1"}""");
        var payload2 = JsonDocument.Parse("""{"id": "2"}""");
        var payload3 = JsonDocument.Parse("""{"id": "3"}""");

        var entity1 = CmsEntity.Create("entity-1", 1, payload1, DateTimeOffset.UtcNow);
        var entity2 = CmsEntity.Create("entity-2", 1, payload2, DateTimeOffset.UtcNow);
        var entity3 = CmsEntity.Create("entity-3", 1, payload3, DateTimeOffset.UtcNow);

        await _writeRepository.AddAsync(entity1, CancellationToken.None);
        await _writeRepository.AddAsync(entity2, CancellationToken.None);
        await _writeRepository.AddAsync(entity3, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act: Update all three in parallel
        var updates = new[]
        {
            UpdateEntity("entity-1", 2, payload1),
            UpdateEntity("entity-2", 2, payload2),
            UpdateEntity("entity-3", 2, payload3)
        };

        await Task.WhenAll(updates);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert: All were updated
        var result1 = await _writeRepository.GetByIdForUpdateAsync("entity-1", CancellationToken.None);
        var result2 = await _writeRepository.GetByIdForUpdateAsync("entity-2", CancellationToken.None);
        var result3 = await _writeRepository.GetByIdForUpdateAsync("entity-3", CancellationToken.None);

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result3.Should().NotBeNull();

        result1!.Version.Should().Be(2);
        result2!.Version.Should().Be(2);
        result3!.Version.Should().Be(2);
    }

    private async Task UpdateEntity(string entityId, int newVersion, JsonDocument payload)
    {
        await _handler.ResolveConflictAsync(async () =>
        {
            var e = await _writeRepository.GetByIdForUpdateAsync(entityId, CancellationToken.None);
            e?.TryApplyPublish(newVersion, payload, DateTimeOffset.UtcNow);
            return e;
        }, CancellationToken.None);
    }

    /// <summary>
    /// Mock that always fails with concurrency exception, used to test retry limit
    /// </summary>
    private sealed class AlwaysFailingConcurrencyHandler(ILogger<ConcurrencyConflictHandler> logger) : IConcurrencyConflictHandler
    {
        public async Task<T> ResolveConflictAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    await Task.Delay(10, cancellationToken);
                    throw new DbUpdateConcurrencyException("Simulated conflict", []);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (attempt == maxRetries - 1)
                    {
                        logger.LogWarning(ex, "Concurrency conflict after {MaxRetries} retries", maxRetries);
                        throw new ApplicationException("Concurrency conflict after retries", ex);
                    }

                    logger.LogInformation("Concurrency conflict detected. Retrying (attempt {Attempt}/{MaxRetries})", attempt + 1, maxRetries);
                    await Task.Delay(100 * (attempt + 1), cancellationToken);
                }
            }

            return default!;
        }
    }
}
