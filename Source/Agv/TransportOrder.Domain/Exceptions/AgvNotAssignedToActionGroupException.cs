namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception raised, when an agv is not assigned to the given action group.
/// </summary>
public class AgvNotAssignedToActionGroupException : Exception
{
    /// <inheritdoc cref="AgvNotAssignedToActionGroupException"/>>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="actionGroupCorrelationId">The action group correlation id.</param>
    public AgvNotAssignedToActionGroupException(Guid agvCorrelationId, Guid actionGroupCorrelationId):base($" Agv {agvCorrelationId} not assigned to action group {actionGroupCorrelationId}"){}

    /// <inheritdoc cref="AgvNotAssignedToActionGroupException"/>>
    /// <param name="description">The description of the failure.</param>
    public AgvNotAssignedToActionGroupException(string description) : base(description) { }
}