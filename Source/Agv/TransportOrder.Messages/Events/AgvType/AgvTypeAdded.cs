using Gebhardt.Acc.Common.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.AgvType;

/// <summary>
/// Represents the event raised, when a new agv type has been created.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv type.</param>
/// <param name="AgvType">The created agv type.</param>
public record AgvTypeAdded(Guid CorrelationId, AgvTypeInfo AgvType) : CorrelatedBy<Guid>;