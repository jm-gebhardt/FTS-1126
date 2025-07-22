using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Node;

/// <summary>
/// Provides a command to update the pallet jam state of a station.
/// </summary>
/// <param name="CorrelationId">The id of the station.</param>
/// <param name="HasPalletJam">True, if the station has a pallet jam.</param>
public record UpdateStationJamState(Guid CorrelationId, bool HasPalletJam) : CorrelatedBy<Guid>;