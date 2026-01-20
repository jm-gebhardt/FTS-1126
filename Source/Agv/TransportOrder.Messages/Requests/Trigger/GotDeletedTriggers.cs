namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Represents the response to the <see cref="GetDeletedTriggers"/> request.
/// </summary>
/// <param name="Triggers">A collection of deleted triggers.</param>
public record GotDeletedTriggers(ICollection<Domain.Models.Trigger.Trigger> Triggers);