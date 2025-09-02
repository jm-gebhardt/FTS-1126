using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents the event raised when an agv was removed from previously reserved zones.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="Zones">The removed zones.</param>
public record AgvRemovedFromZones(Guid CorrelationId, ICollection<string> Zones) : CorrelatedBy<Guid>;