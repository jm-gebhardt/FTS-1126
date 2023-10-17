using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the Agv.IsOnline has changed.
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
/// <param name="IsNowOnline"></param>
[PublicAPI]
public record AgvOnlineChanged(Guid CorrelationId, bool IsNowOnline) : CorrelatedBy<Guid>;