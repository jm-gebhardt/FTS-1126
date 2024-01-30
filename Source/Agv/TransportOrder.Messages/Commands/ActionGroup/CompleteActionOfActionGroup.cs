using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to complete an individual action of an action group.
/// </summary>
/// <param name="ActionCorrelationId">The correlation id o the action.</param>
/// <param name="ActionGroupCorrelationId">The correlation id o the action group.</param>
public record CompleteActionOfActionGroup(Guid ActionCorrelationId, Guid ActionGroupCorrelationId) : CorrelatedBy<Guid>
{
    public Guid CorrelationId => ActionGroupCorrelationId;
}