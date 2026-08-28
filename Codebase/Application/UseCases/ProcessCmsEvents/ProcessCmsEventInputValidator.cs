using FluentValidation;

namespace CmsEvents.Application.UseCases.ProcessCmsEvents;

public sealed class ProcessCmsEventInputValidator : AbstractValidator<ProcessCmsEventsInput>
{
    public ProcessCmsEventInputValidator()
    {
        RuleFor(x => x.Type).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Timestamp).NotEmpty();
        RuleFor(x => x.Version).GreaterThanOrEqualTo(1).When(x => x.Version.HasValue);

        When(x => IsPublish(x) || IsUnpublish(x), () =>
        {
            RuleFor(x => x.Payload).NotNull();
            RuleFor(x => x.Version).NotNull();
        });
    }

    private static bool IsPublish(ProcessCmsEventsInput x) => x.Type.Equals(CmsEventTypes.Publish, StringComparison.OrdinalIgnoreCase);
    private static bool IsUnpublish(ProcessCmsEventsInput x) => x.Type.Equals(CmsEventTypes.Unpublish, StringComparison.OrdinalIgnoreCase);
}