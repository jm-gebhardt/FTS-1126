using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised when agv has been maintained.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Maintenance">The maintenance.</param>
public record AgvMaintained(Guid CorrelationId, Maintenance Maintenance):CorrelatedBy<Guid>;