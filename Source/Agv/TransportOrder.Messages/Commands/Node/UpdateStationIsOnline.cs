using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Node;

/// <summary>
/// Provides a command to update the online state of a station.
/// </summary>
/// <param name="CorrelationId">The id of the station.</param>
/// <param name="IsOnline">If the station is now online.</param>
public record UpdateStationIsOnline(Guid CorrelationId, bool IsOnline) : CorrelatedBy<Guid>;