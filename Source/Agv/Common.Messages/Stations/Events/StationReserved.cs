using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Stations.Events;

/// <summary>
/// Represents the event raised when a station has been reserved.
/// </summary>
/// <param name="StationId">The Id of the station.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
public record StationReserved(Guid StationId, Guid AgvCorrelationId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{Key}"/>>
    public Guid CorrelationId => StationId;
}