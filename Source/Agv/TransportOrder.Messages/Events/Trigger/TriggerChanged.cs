namespace Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;

/// <summary>
/// Represents the even raised when a trigger changed.
/// </summary>
/// <param name="Trigger">The trigger.</param>
public record TriggerChanged(Domain.Models.Trigger.Trigger Trigger);