using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised when the agv went offline.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="Timestamp">The timestamp.</param>
[PublicAPI]
public record AgvWentOffline(Guid CorrelationId, DateTime Timestamp) : CorrelatedBy<Guid>;