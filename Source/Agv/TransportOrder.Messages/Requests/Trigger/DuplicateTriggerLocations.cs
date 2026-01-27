using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Provides a request for duplicating all locations of a trigger to a target trigger.
/// </summary>
/// <param name="CorrelationId">The correlation id of the trigger that is localized.</param>
/// <param name="TargetTriggerCorrelationId">The correlation id of the trigger that is supposed to use the same location.</param>
public record DuplicateTriggerLocations(Guid CorrelationId, Guid TargetTriggerCorrelationId) : CorrelatedBy<Guid>;