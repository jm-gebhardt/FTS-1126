namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Represents the event raised when a picking agv passed the Doppelspiel trigger point.
/// </summary>
/// <param name="PickActionTarget">The target of the action groups pick action.</param>
/// <param name="AgvCorrelationId">The agv correlation id.</param>
/// <param name="HostOrderCorrelationId">The correlation id of the host order of the pick.</param>
public record PickingAgvPassedDoppelspielTriggerPoint(string PickActionTarget, Guid AgvCorrelationId, Guid HostOrderCorrelationId);