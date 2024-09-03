namespace Gebhardt.Acc.KaeserSidecar.Messages;

/// <summary>
/// Represents the event raised when an agv started a pick on the short loop.
/// </summary>
/// <param name="OrderId">The correlation id of the oder.</param>
/// <param name="Station">The pick station.</param>
/// <param name="TargetStation">The target station.</param>
public record AgvStartedShortLoopPick(string TargetStation, Guid OrderId, string Station);