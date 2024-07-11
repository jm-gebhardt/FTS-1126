namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Provides a request to get a trigger.
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
public record GetTrigger(Guid TriggerId);