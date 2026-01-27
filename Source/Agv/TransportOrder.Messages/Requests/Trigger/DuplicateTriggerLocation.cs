using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Provides a request for duplicating the location of a trigger to a target trigger.
/// </summary>
/// <param name="CorrelationId">The correlation id of the trigger that is localized.</param>
/// <param name="TargetTriggerCorrelationId">The correlation id of the trigger that is supposed to use the same location.</param>
/// <param name="LocationId">The id of the location.</param>
public record DuplicateTriggerLocation(Guid CorrelationId, Guid TargetTriggerCorrelationId, Guid LocationId): CorrelatedBy<Guid>;