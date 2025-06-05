namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;

/// <summary>
/// Provides a command to queue the next pick order at an akl station.
/// </summary>
/// <param name="PickTarget">The pick target.</param>
/// <param name="AssignedAgv">The agv to assigned to the order.</param>
public record TryQueueNextAklPick(string PickTarget, Guid AssignedAgv);