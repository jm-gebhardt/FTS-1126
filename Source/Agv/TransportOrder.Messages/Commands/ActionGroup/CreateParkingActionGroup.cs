using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to create a parking <see cref="ActionGroup"/> for an agv.
/// </summary>
/// <param name="CorrelationId">The correlationId of the agv.</param>
public record CreateParkingActionGroup(Guid CorrelationId) : CorrelatedBy<Guid>;