namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Represents the response to the <see cref="GetTriggers"/> request.
/// </summary>
/// <param name="Triggers">A collection of triggers.</param>
public record GotTriggers(ICollection<Domain.Models.Trigger.Trigger> Triggers);