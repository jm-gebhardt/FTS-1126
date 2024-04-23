namespace Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event raised when aborting an action group failed.
/// </summary>
/// <param name="Reason">The reason for the failure.</param>
public record AbortActionGroupFailed(string Reason);