using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices.Housekeeping;

/// <inheritdoc/>
public class LogHousekeepingAction(ILogEntryRepository repository) : IHousekeepingAction
{
    /// <inheritdoc/>
    public HousekeepingEntityType EntityType => HousekeepingEntityType.Logs;

    /// <inheritdoc/>
    public Task ExecuteAsync(DateTime outdatedTime, CancellationToken cancellationToken = default)
        => repository.DeleteOutdatedLogs(outdatedTime);
}
