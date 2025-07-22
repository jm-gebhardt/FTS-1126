using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Stations.Events;

/// <summary>
/// Represents the event raised, when the jam state of a station changed.
/// </summary>
/// <param name="StationName">The name of the station.</param>
/// <param name="HasPalletJam">True, if the station has a pallet jam.</param>
public record JamStateUpdated(string StationName, bool HasPalletJam) : CorrelatedBy<string>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public string CorrelationId => StationName;
}