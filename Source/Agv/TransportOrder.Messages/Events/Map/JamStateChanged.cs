using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Map;

/// <summary>
/// Represents the event raised when a pallet jam state of a station has been chan
/// </summary>
/// <param name="CorrelationId">The id of the station.</param>
/// <param name="HasPalletJam">True, if the station has a pallet jam.</param>
public record JamStateChanged(Guid CorrelationId, bool HasPalletJam): CorrelatedBy<Guid>;