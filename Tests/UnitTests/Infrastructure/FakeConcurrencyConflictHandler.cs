using CmsEvents.Application.Persistence.Abstractions;

namespace CmsEvents.UnitTests.Infrastructure;

/// <summary>
/// Fake implementation of concurrency conflict handler for unit tests.
/// By default, it passes through operations without retry logic.
/// Can be configured to simulate conflicts if needed.
/// </summary>
public sealed class FakeConcurrencyConflictHandler : IConcurrencyConflictHandler
{
    public bool ShouldFail { get; set; }
    public int SimulatedConflictCount { get; set; }

    public async Task<T> ResolveConflictAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        if (ShouldFail)
        {
            throw new ApplicationException("Simulated concurrency conflict handler failure");
        }

        return await operation();
    }
}