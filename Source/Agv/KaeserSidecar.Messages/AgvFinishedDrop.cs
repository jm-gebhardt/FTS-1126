namespace Gebhardt.Acc.KaeserSidecar.Messages;

/// <summary>
/// Represents the event raised when an agv finished a drop.
/// </summary>
/// <param name="OrderId">The correlation id of the order.</param>
/// <param name="Station">The drop station.</param>
public record AgvFinishedDrop(Guid OrderId, string Station);