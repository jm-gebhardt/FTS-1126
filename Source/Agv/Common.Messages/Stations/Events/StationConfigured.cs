using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Stations.Events;

/// <summary>
/// Represents the event raised when a station has been configured.
/// </summary>
/// <param name="Id">The id of the station.</param>
/// <param name="StationProperties">The station properties.</param>
public record StationConfigured(Guid Id, Dictionary<string, object?> StationProperties) : CorrelatedBy<Guid>
{
    /// <summary>
    /// The correlation id of the message (station id).
    /// </summary>
    public Guid CorrelationId => Id;
}