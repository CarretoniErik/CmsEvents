namespace CmsEvents.Application.UseCases.ProcessCmsEvents;

public enum EventOutcome { Applied, Ignored, Deleted, Failed }

public sealed class ProcessBatchResult
{
    public static ProcessBatchResult Empty => new();

    public int Applied { get; private set; }
    public int Ignored { get; private set; }
    public int Deleted { get; private set; }
    public int Failed { get; private set; }

    public void Register(EventOutcome outcome)
    {
        switch (outcome)
        {
            case EventOutcome.Applied: Applied++; break;
            case EventOutcome.Ignored: Ignored++; break;
            case EventOutcome.Deleted: Deleted++; break;
            case EventOutcome.Failed: Failed++; break;
        }
    }
}