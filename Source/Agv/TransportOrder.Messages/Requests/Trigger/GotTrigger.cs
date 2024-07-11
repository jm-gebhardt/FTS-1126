namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Represents the response to <see cref="GetTrigger"/>
/// </summary>
/// <param name="Trigger">The trigger.</param>
public record GotTrigger(Domain.Models.Trigger.Trigger Trigger);