using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the Agv has been removed from the system
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
[PublicAPI]
public record AgvRemoved(Guid CorrelationId) : CorrelatedBy<Guid>;