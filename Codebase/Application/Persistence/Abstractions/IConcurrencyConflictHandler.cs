namespace CmsEvents.Application.Persistence.Abstractions;

public interface IConcurrencyConflictHandler
{
    Task<T> ResolveConflictAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken);
}