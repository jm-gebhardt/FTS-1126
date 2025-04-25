namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;

/// <summary>
/// Provides a command to try to queue the next delayed short circuit order for group of the given station.
/// </summary>
/// <param name="Source">The source of the station.</param>
/// <param name="Target">The target of the station.</param>
public record TryQueueDelayedOrder(string Source, string Target);