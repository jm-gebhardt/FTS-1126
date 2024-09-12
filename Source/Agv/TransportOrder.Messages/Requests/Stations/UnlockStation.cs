using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Represents a request to unlock a station.
/// </summary>
/// <param name="CorrelationId">The station to unlock.</param>
public record UnlockStation(Guid CorrelationId): CorrelatedBy<Guid>;