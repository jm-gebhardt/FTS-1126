using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;

/// <summary>
/// Command to queue an ActionGroup
/// </summary>
/// <param name="ActionGroup"></param>
public record QueueActionGroup(ActionGroup ActionGroup) : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId => ActionGroup.CorrelationId;
}