using MassTransit;

namespace Gebhardt.Acc.Common.Messages.HostOrder.Events;

/// <summary>
/// Represents the event raised when cargo reporting has been requested
/// </summary>
/// <param name="CorrelationId">The correlation id of the order.</param>
/// <param name="HasCargo">True if the agv has cargo.</param>
public record CargoReportingRequested(Guid CorrelationId, bool HasCargo) : CorrelatedBy<Guid>;