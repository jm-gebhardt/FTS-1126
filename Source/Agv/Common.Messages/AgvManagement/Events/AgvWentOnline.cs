using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents the event raised when an agv went online.
/// </summary>
/// <param name="CorrelationId"> The correlation id of the agv that went online.</param>
public record AgvWentOnline(Guid CorrelationId): CorrelatedBy<Guid>;