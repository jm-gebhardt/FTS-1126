namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;

/// <summary>
/// Provides a command to queue unmatched Doppelspiel pick orders.
/// </summary>
/// <param name="Station">The name of the station for which to queue the picks.</param>
public record QueueUnmatchedDoppelspielPicks(string Station);