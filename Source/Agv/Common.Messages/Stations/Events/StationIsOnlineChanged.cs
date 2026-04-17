using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Stations.Events;

/// <summary>
/// Represents the event raised when a station's reachability changes.
/// </summary>
/// <param name="StationId">The id of the station.</param>
/// <param name="IsOnline">True, when the station went online.</param>
public record StationIsOnlineChanged(Guid StationId, bool IsOnline) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => StationId;
}