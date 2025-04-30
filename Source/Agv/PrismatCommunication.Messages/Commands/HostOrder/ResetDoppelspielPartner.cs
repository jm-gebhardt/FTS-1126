
namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;

/// <summary>
/// Represents a command sent when a doppelspiel partner needs a different Agv.
/// </summary>
/// <param name="ActionGroupCorrelationId">The correlation id of the action group.</param>
public record ResetDoppelspielPartner(Guid ActionGroupCorrelationId);