using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event raised when an action group has been resumed.
/// </summary>
/// <param name="ActionGroupCorrelationId">The action group correlation id</param>
/// <param name="AgvCorrelationId">The assigned agv correlation id.</param>
/// <param name="InitiatingUser">User that resumed the action group.</param>
public record ActionGroupResumed([PublicAPI] Guid ActionGroupCorrelationId, Guid AgvCorrelationId, string? InitiatingUser);