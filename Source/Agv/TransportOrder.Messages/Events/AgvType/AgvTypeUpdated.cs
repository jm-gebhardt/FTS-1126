using Gebhardt.Acc.Common.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.AgvType;

/// <summary>
/// Represents the event raise when an agv type has been updated.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv type.</param>
/// <param name="AgvType">The agv type.</param>
public record AgvTypeUpdated(Guid CorrelationId, AgvTypeInfo AgvType) : CorrelatedBy<Guid>;