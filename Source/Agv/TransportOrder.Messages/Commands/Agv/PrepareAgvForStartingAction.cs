using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to start an action at the assigned agv.
/// </summary>
/// <param name="Action">The action.</param>
/// <param name="CorrelationId">The assigned agv correlation id.</param>
public record PrepareAgvForStartingAction(Domain.Models.ActionGroup.AgvAction Action, Guid CorrelationId): CorrelatedBy<Guid>;