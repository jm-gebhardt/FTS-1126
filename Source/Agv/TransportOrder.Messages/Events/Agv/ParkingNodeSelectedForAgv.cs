using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised when a parking node was selected for an agv.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="ParkingNode">The parking node.</param>
public record ParkingNodeSelectedForAgv(Guid CorrelationId, string ParkingNode): CorrelatedBy<Guid>;