using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// The charging agv can handle actions again event.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record ChargingAgvCanHandleActionsAgain(Guid CorrelationId) : CorrelatedBy<Guid>;