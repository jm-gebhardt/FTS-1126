namespace Gebhardt.Acc.PrismatCommunication.Messages.Events;

/// <summary>
/// Represents the event raised when the load trigger point has been passed.
/// </summary>
/// <param name="Agv">The agv that passed the trigger point.</param>
public record LoadTriggerPointPassed(Guid Agv);