namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands;

/// <summary>
/// Internal Message to signal canceling of host order.
/// </summary>
/// <param name="Hu">The handling unit.</param>
public record CancelHostOrder(string Hu);