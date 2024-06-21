using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents the event raised when the load trigger point has been passed by an agv.
/// </summary>
/// <param name="CorrelationId">The correlationId of the agv.</param>
/// <param name="ActionGroupCorrelationId">The correlationId of the actionGroup (if agv carried load).</param>
public record AgvPassedLoadTriggerPoint(Guid CorrelationId, Guid? ActionGroupCorrelationId): CorrelatedBy<Guid>;