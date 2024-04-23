using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to start an individual <see cref="Action"/> of an action group.
/// </summary>
/// <param name="Action">The action.</param>
/// <param name="AssignedAgv">The agv.</param>
public record StartActionOfActionGroup(AgvAction Action, Guid AssignedAgv) : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId => Action.ParentId;
}