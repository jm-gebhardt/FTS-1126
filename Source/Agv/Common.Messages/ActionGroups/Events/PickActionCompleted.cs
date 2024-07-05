using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents the event raised when a pick action has been completed.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="ActionCorrelationId">The correlation id of the pick action.</param>
/// <param name="Timestamp">The time of completion.</param>
public record PickActionCompleted(Guid CorrelationId, Guid ActionCorrelationId, DateTime Timestamp) : CorrelatedBy<Guid>;