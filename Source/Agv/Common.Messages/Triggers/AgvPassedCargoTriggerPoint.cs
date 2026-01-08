using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Represents the event raised when the cargo trigger point has been passed by an agv.
/// </summary>
/// <param name="CorrelationId">The correlationId of the agv.</param>
/// <param name="ActionGroupCorrelationId">The correlationId of the actionGroup (if agv carried cargo).</param>
/// <param name="Action">The current action.</param>
/// <param name="HasCargo">Flag if agv has cargo loaded or not.</param>
[PublicAPI]
public record AgvPassedCargoTriggerPoint(Guid CorrelationId, Guid? ActionGroupCorrelationId, AgvAction? Action, bool HasCargo): CorrelatedBy<Guid>;