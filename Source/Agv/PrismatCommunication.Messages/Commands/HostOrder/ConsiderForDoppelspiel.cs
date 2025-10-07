using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;

/// <summary>
/// Provides a command to consider an order for Doppelspiel.
/// </summary>
/// <param name="CorrelationId">The order correlation id.</param>
public record ConsiderForDoppelspiel(Guid CorrelationId) : CorrelatedBy<Guid>;