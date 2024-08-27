namespace Gebhardt.Acc.PrismatCommunication.Messages.Events;

/// <summary>
/// Represents the event raised when an agv dropping at 1050 passed the short loop trigger point.
/// </summary>
/// <param name="Agv">The agv.</param>
/// <param name="ShouldNotPickNext">The agv should not pick in the next step.</param>
public record ShortLoopTriggerPointPassed(Guid Agv, bool ShouldNotPickNext);