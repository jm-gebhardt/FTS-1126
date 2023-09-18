using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Internal scheduled message to signal that AGV seems to be offline
/// </summary>
/// <param name="CorrelationId">Inherited</param>
/// <param name="Created">UTC timestamp when the scheduled message has been created</param>
[PublicAPI]
public record AgvOfflineMessage(Guid CorrelationId, DateTime Created) : CorrelatedBy<Guid>;