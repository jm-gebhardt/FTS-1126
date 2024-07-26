namespace Gebhardt.Acc.PrismatCommunication.Messages.Events;

/// <summary>
/// Represents the event raised when an agv dropping at 1050 passed the short circle trigger point.
/// </summary>
/// <param name="Agv">The agv.</param>
public record ShortCircleTriggerPointPassed(Guid Agv);