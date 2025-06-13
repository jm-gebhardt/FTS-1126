using Gebhardt.Acc.Common.Messages.ActionGroups;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to complete an individual action of an action group.
/// </summary>
/// <param name="ActionCorrelationId">The correlation id o the action.</param>
/// <param name="ActionGroupCorrelationId">The correlation id o the action group.</param>
/// <param name="Creator">The creator of the command.</param>
public record CompleteActionOfActionGroup(Guid ActionCorrelationId, Guid ActionGroupCorrelationId, ActionGroupCreator Creator = ActionGroupCreator.TransportOrder) : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId => ActionGroupCorrelationId;
}