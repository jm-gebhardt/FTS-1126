using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Stations.Events;

/// <summary>
/// Represents the event raised, when the jam state of a station changed.
/// </summary>
/// <param name="StationId">The Id of the station.</param>
/// <param name="HasPalletJam">True, if the station has a pallet jam.</param>
public record JamStateUpdated(Guid StationId, bool HasPalletJam) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => StationId;
}