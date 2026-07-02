using Gebhardt.Acc.PrismatCommunication.Domain.Models.Housekeeping;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;

namespace Gebhardt.Acc.PrismatCommunication.Services.BackgroundServices.Housekeeping;

/// <inheritdoc/>
public class HostOrderHousekeepingAction(IHostOrderRepository repository) : IHousekeepingAction
{
    /// <inheritdoc/>
    public HousekeepingEntityType EntityType => HousekeepingEntityType.Orders;

    /// <inheritdoc/>
    public Task ExecuteAsync(DateTime outdatedTime, CancellationToken cancellationToken = default)
        => repository.DeleteOutdatedOrdersAsync(outdatedTime);
}