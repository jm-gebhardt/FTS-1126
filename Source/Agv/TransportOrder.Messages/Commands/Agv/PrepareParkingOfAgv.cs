using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to prepare parking of an agv.
/// </summary>
/// <param name="CorrelationId">The correlationId of the agv.</param>
public record PrepareParkingOfAgv(Guid CorrelationId) : CorrelatedBy<Guid>;