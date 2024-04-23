using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Event raised when an ActionGroup has been estimated.
/// </summary>
/// <param name="Action">The action of the ActionGroup.</param>
/// <param name="AgvId">The correlationId of the assigned Agv.</param>
/// <param name="ActionGroupId">The correlationId of the ActionGroup.</param>
public record ActionDurationEstimated(AgvAction Action, Guid AgvId, Guid ActionGroupId) : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId => ActionGroupId;
}