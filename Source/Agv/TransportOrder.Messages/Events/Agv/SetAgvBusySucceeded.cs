using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// AGV reservation request was successful.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
[PublicAPI]
public record SetAgvBusySucceeded(Guid CorrelationId) : CorrelatedBy<Guid>;