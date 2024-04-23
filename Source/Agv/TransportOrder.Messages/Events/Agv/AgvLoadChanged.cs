using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the current load of an AGV has changed
/// </summary>
/// <param name="CorrelationId">The correlationId of the Agv</param>
/// <param name="IsNowLoaded">true if Agv has load</param>
[PublicAPI]
public record AgvLoadChanged(Guid CorrelationId, bool IsNowLoaded) : CorrelatedBy<Guid>;