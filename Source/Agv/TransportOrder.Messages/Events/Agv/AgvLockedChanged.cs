using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the Agv.IsLocked has changed.
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
/// <param name="IsNowLocked"></param>
[PublicAPI]
public record AgvLockedChanged(Guid CorrelationId, bool IsNowLocked) : CorrelatedBy<Guid>;