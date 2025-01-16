namespace Gebhardt.Acc.KaeserSidecar.Messages;

/// <summary>
/// Represents the event raised when an agv finished a pick.
/// </summary>
/// <param name="OrderId">The correlation id of the order.</param>
/// <param name="Station">The pick station.</param>
public record AgvFinishedPick(Guid OrderId, string Station);