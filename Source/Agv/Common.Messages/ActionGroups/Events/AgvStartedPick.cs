using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents the event raised when an agv started a pick.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="TargetStation">The pick target station.</param>
public record AgvStartedPick(string TargetStation, Guid CorrelationId) : CorrelatedBy<Guid>;