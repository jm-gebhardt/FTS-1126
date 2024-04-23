using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Command to estimate the duration of an ActionGroup.
/// </summary>
/// <param name="Action">The first Action of the ActionGroup.</param>
/// <param name="AgvId">The correlationId of the assigned Agv.</param>
/// <param name="ActionGroupId">The correlationId of the ActionGroup.</param>
public record EstimateActionGroupDuration(AgvAction Action, Guid AgvId, Guid ActionGroupId) : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId => ActionGroupId;
}