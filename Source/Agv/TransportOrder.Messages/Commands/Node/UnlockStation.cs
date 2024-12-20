using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Node;

/// <summary>
/// Provides a command to unlock a station.
/// </summary>
/// <param name="CorrelationId">The station to unlock.</param>
/// <param name="User">The user that requested the unlocking.</param>
public record UnlockStation(Guid CorrelationId, string User): CorrelatedBy<Guid>;