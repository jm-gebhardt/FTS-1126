using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command command to handle the failure of starting the next action
/// of an action group assigned to an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record HandleAgvActionStartFailure(Guid CorrelationId) : CorrelatedBy<Guid>;