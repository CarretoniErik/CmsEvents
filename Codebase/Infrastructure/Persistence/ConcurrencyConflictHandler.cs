using CmsEvents.Application.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CmsEvents.Infrastructure.Persistence;

public sealed class ConcurrencyConflictHandler(ILogger<ConcurrencyConflictHandler> logger) : IConcurrencyConflictHandler
{
    private const int MaxRetries = 3;

    public async Task<T> ResolveConflictAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (attempt == MaxRetries - 1)
                {
                    logger.LogWarning(ex, "Concurrency conflict after {MaxRetries} retries", MaxRetries);
                    throw new ApplicationException("Concurrency conflict after retries", ex);
                }

                logger.LogInformation("Concurrency conflict detected. Retrying (attempt {Attempt}/{MaxRetries})", attempt + 1, MaxRetries);
                await Task.Delay(100 * (attempt + 1), cancellationToken);
            }
        }

        return default!;
    }
}