using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Map;

/// <summary>
/// Represents the event raised when a pallet jam state of a station has changed.
/// </summary>
/// <param name="CorrelationId">The id of the station.</param>
/// <param name="HasPalletJam">True, if the station has a pallet jam.</param>
/// <param name="LastChangedDate">The time the jam state changed last.</param>
/// <param name="PreviousHasPalletJam">The previous jam state.</param>
/// <param name="CurrentChangedDate">The current jam state change time.</param>
public record JamStateChanged(Guid CorrelationId, bool HasPalletJam, DateTime LastChangedDate, bool PreviousHasPalletJam, DateTime CurrentChangedDate) : CorrelatedBy<Guid>;