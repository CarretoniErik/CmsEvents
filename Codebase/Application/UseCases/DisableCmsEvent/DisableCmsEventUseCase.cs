using CmsEvents.Application.Persistence;
using CmsEvents.Application.Persistence.Abstractions;

namespace CmsEvents.Application.UseCases.DisableCmsEvent;

public sealed class DisableCmsEventUseCase(ICmsEntityWriteRepository writeRepository, IUnitOfWork unitOfWork) : IDisableCmsEventUseCase
{
    public async Task<DisableCmsEventResult> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        var entity = await writeRepository.GetByIdForUpdateAsync(id, cancellationToken);

        if (entity is null) return DisableCmsEventResult.NotFound;

        entity.DisableByAdmin();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return DisableCmsEventResult.Success;
    }
}